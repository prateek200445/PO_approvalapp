using System.Globalization;
using System.Text.RegularExpressions;
using Dapper;

namespace POApprovalAPI.Services;

/// <summary>
/// New employee entry into payroll Loginentry.empinfo — same table the ERP "Employee Information" form writes.
/// Values are validated against the live column types/lengths so ERP columns never overflow.
/// </summary>
public sealed class HrEmployeeMasterService
{
    private static readonly Regex EmpCodePattern = new(@"^([A-Za-z]+)(\d+)$", RegexOptions.Compiled);

    private readonly DatabaseService _database;

    public HrEmployeeMasterService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<HrEmployeeFormOptionsDto> GetFormOptionsAsync()
    {
        using var connection = _database.CreatePayrollLoginEntryConnection();

        var companyBranches = (await connection.QueryAsync<HrCompanyBranchDto>(@"
SELECT LTRIM(RTRIM(CompanyName)) AS CompanyName, LTRIM(RTRIM(Branch)) AS Branch, COUNT(*) AS Employees
FROM empinfo WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM(CompanyName)), '') <> ''
  AND ISNULL(LTRIM(RTRIM(Branch)), '') <> ''
  AND LOWER(LTRIM(RTRIM(ISNULL(isactive, 'yes')))) = 'yes'
GROUP BY LTRIM(RTRIM(CompanyName)), LTRIM(RTRIM(Branch))
ORDER BY LTRIM(RTRIM(CompanyName)), COUNT(*) DESC", commandTimeout: 60)).ToList();

        async Task<List<string>> Distinct(string column)
        {
            var rows = await connection.QueryAsync<string>($@"
SELECT LTRIM(RTRIM({column})) AS V
FROM empinfo WITH (NOLOCK)
WHERE ISNULL(LTRIM(RTRIM({column})), '') NOT IN ('', '0', '00', '-', '.', '*')
  AND LOWER(LTRIM(RTRIM(ISNULL(isactive, 'yes')))) = 'yes'
GROUP BY LTRIM(RTRIM({column}))
ORDER BY LTRIM(RTRIM({column}))", commandTimeout: 60);
            return rows.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
        }

        async Task<List<string>> Lookup(string sql)
        {
            try
            {
                var rows = await connection.QueryAsync<string>(sql, commandTimeout: 30);
                return rows.Where(v => !string.IsNullOrWhiteSpace(v)).Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            }
            catch
            {
                return [];
            }
        }

        var designations = await Lookup("SELECT LTRIM(RTRIM(Designation)) FROM Designationmaster WITH (NOLOCK) ORDER BY LTRIM(RTRIM(Designation))");
        var categories = await Lookup("SELECT LTRIM(RTRIM(CategoryName)) FROM CategoryMaster WITH (NOLOCK) ORDER BY LTRIM(RTRIM(CategoryName))");
        var jobDescriptions = await Lookup("SELECT LTRIM(RTRIM(JobDesc)) FROM JobDescription WITH (NOLOCK) ORDER BY LTRIM(RTRIM(JobDesc))");

        return new HrEmployeeFormOptionsDto
        {
            CompanyBranches = companyBranches,
            Departments = await Distinct("Deptt"),
            SubDepartments = await Distinct("SubDeptt"),
            Designations = designations.Count > 0 ? designations : await Distinct("Designation"),
            WorkRoles = await Distinct("Workrole"),
            Categories = categories.Count > 0 ? categories : await Distinct("category"),
            JobDescriptions = jobDescriptions,
        };
    }

    /// <summary>
    /// Next EmpCode for a branch. Branches can run several numbering series in parallel under one prefix
    /// (e.g. Polyfilms PPL05xxx / PPL07xxx / PPL10xxx), so recent joiners are grouped into series by
    /// thousand-block. The suggestion continues the series of the latest joiner; the next free code of the
    /// other recent series is returned as alternatives. Codes already present in empinfo are skipped.
    /// </summary>
    public async Task<HrNextEmpCodeDto> SuggestEmpCodeAsync(string branch)
    {
        branch = (branch ?? "").Trim();
        if (branch.Length == 0)
            throw new InvalidOperationException("branch is required.");

        using var connection = _database.CreatePayrollLoginEntryConnection();
        var recent = (await connection.QueryAsync<string>(@"
SELECT TOP 60 LTRIM(RTRIM(EmpCode))
FROM empinfo WITH (NOLOCK)
WHERE LOWER(LTRIM(RTRIM(Branch))) = LOWER(@Branch)
  AND DateOJ <= DATEADD(day, 1, GETDATE())
ORDER BY DateOJ DESC, EmpCode DESC", new { Branch = branch }, commandTimeout: 30))
            .Select(c => EmpCodePattern.Match(c ?? ""))
            .Where(m => m.Success && m.Groups[2].Value.Length <= 12)
            .Select(m => (Prefix: m.Groups[1].Value, Digits: m.Groups[2].Value, Number: long.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)))
            .ToList();

        if (recent.Count == 0)
            return new HrNextEmpCodeDto { Branch = branch };

        var prefix = recent
            .GroupBy(x => x.Prefix, StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(g => g.Count())
            .First().First().Prefix;

        var series = recent
            .Where(x => string.Equals(x.Prefix, prefix, StringComparison.OrdinalIgnoreCase))
            .GroupBy(x => (Block: x.Number / 1000, Width: x.Digits.Length))
            .Select(g => (g.Key.Width, Max: g.Max(x => x.Number), Count: g.Count()))
            .ToList();

        var taken = (await connection.QueryAsync<string>(@"
SELECT LTRIM(RTRIM(EmpCode)) FROM empinfo WITH (NOLOCK) WHERE EmpCode LIKE @Like",
            new { Like = prefix + "%" }, commandTimeout: 30))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        string Format(long n, int width) => prefix + n.ToString(CultureInfo.InvariantCulture).PadLeft(width, '0');
        string NextFree(long max, int width)
        {
            var n = max + 1;
            while (taken.Contains(Format(n, width)))
                n++;
            return Format(n, width);
        }

        // `series` keeps first-seen order, i.e. the series of the most recent joiner comes first.
        var primary = series[0];
        return new HrNextEmpCodeDto
        {
            Branch = branch,
            Prefix = prefix,
            LastEmpCode = Format(primary.Max, primary.Width),
            SuggestedEmpCode = NextFree(primary.Max, primary.Width),
            Alternatives = series.Skip(1)
                .Where(s => s.Count >= 2)
                .Select(s => new HrEmpCodeSeriesDto
                {
                    LastEmpCode = Format(s.Max, s.Width),
                    NextEmpCode = NextFree(s.Max, s.Width),
                })
                .ToList(),
        };
    }

    public async Task<HrCreateEmployeeResultDto> CreateEmployeeAsync(HrCreateEmployeeRequest r, string createdBy)
    {
        var empCode = (r.EmpCode ?? "").Trim();
        var missing = new List<string>();
        if (empCode.Length == 0) missing.Add("Employee Code");
        if (string.IsNullOrWhiteSpace(r.Name)) missing.Add("Employee Name");
        if (string.IsNullOrWhiteSpace(r.FatherName)) missing.Add("Father's Name");
        if (string.IsNullOrWhiteSpace(r.CompanyName)) missing.Add("Company Name");
        if (string.IsNullOrWhiteSpace(r.Branch)) missing.Add("Branch");
        if (string.IsNullOrWhiteSpace(r.DateOfJoining)) missing.Add("Date of Joining");
        if (string.IsNullOrWhiteSpace(r.Gender)) missing.Add("Gender");
        if (missing.Count > 0)
            throw new InvalidOperationException("Required: " + string.Join(", ", missing) + ".");
        if (empCode.Any(char.IsWhiteSpace))
            throw new InvalidOperationException("Employee Code cannot contain spaces.");

        static string YesNo(bool? v) => v == true ? "yes" : "no";
        static string YesNoTitle(bool? v) => v == true ? "Yes" : "No";

        var pfApplicable = r.PfApplicable ?? true;
        var fields = new List<(string Column, string Label, object? Value)>
        {
            ("EmpCode", "Employee Code", empCode),
            ("Name", "Employee Name", r.Name),
            ("Fathername", "Father's Name", r.FatherName),
            ("mothername", "Mother's Name", r.MotherName),
            ("spousename", "Spouse Name", r.SpouseName),
            ("noofchild", "No. of Children", r.NoOfChildren),
            ("DOB", "Date of Birth", r.DateOfBirth),
            ("Sex", "Gender", r.Gender),
            ("mrg_status", "Marital Status", r.MaritalStatus),
            ("mthr_tng", "Language", r.Language),
            ("religion", "Religion", r.Religion),
            ("caste", "Caste", r.Caste),
            ("nationality", "Nationality", string.IsNullOrWhiteSpace(r.Nationality) ? "INDIAN" : r.Nationality),
            ("handicap", "Handicapped", r.Handicapped == true ? "1" : "0"),
            ("Blood", "Blood Group", r.BloodGroup),
            ("pasportno", "Passport No", r.PassportNo),
            ("pasportdtfrm", "Passport Valid From", r.PassportValidFrom),
            ("pasportdtto", "Passport Valid To", r.PassportValidTo),
            ("CardID", "Card Id", r.CardId),
            ("JobDescription", "Job Description", r.JobDescription),
            ("Qualification", "Educational Qualification", r.Qualification),
            ("specialization", "Specialization", r.Specialization),
            ("univrsity", "University", r.University),
            ("pass_year", "Year of Passing", r.YearOfPassing),
            ("skill", "Skills", r.Skills),
            ("ability", "Abilities", r.Abilities),
            ("isdirector", "Is Director", YesNo(r.IsDirector)),

            ("CompanyName", "Company Name", r.CompanyName),
            ("Branch", "Branch", r.Branch),
            ("DateOJ", "Date of Joining", r.DateOfJoining),
            ("cnfrm_dt", "Confirmation Date", r.ConfirmationDate),
            ("Designation", "Designation", r.Designation),
            ("Deptt", "Department", r.Department),
            ("SubDeptt", "Sub Department", r.SubDepartment),
            ("category", "Category", r.Category),
            ("subcategory", "Sub Category", r.SubCategory),
            ("Workrole", "Work Role", r.WorkRole),
            ("WRK_Area", "Work Area", r.WorkArea),
            ("wrk_location", "Work Location", r.WorkLocation),
            ("Experience", "Experience", r.Experience),
            ("prv_employr", "Previous Employer", r.PreviousEmployer),
            ("ContractorName", "Contractor Name", r.ContractorName),
            ("IsHOEmp", "HO Employee", r.IsHoEmp == true ? 1 : 0),
            ("CTC", "CTC", r.Ctc ?? "0"),
            ("IsSalaryPerDay", "Salary Per Day", YesNoTitle(r.IsSalaryPerDay)),
            ("SalaryPerDay", "Salary Per Day Amount", r.SalaryPerDay ?? "0"),
            ("PFApplicability", "PF Applicable", YesNo(pfApplicable)),
            ("PfType", "PF Type", YesNo(pfApplicable)),
            ("PfApplicableDt", "PF Applicable Date", pfApplicable ? (string.IsNullOrWhiteSpace(r.PfApplicableDate) ? r.DateOfJoining : r.PfApplicableDate) : null),
            ("BonusApplicability", "Bonus Applicable", YesNo(r.BonusApplicable)),
            ("HRAApp", "HRA Applicable", YesNo(r.HraApplicable)),
            ("AttBonus", "Attendance Bonus", YesNoTitle(r.AttendanceBonus)),
            ("IsOvertime", "Overtime", YesNoTitle(r.Overtime)),
            ("RotationApp", "Rotation", YesNoTitle(r.Rotation)),
            ("EmpRecog", "Employee Recognition", string.IsNullOrWhiteSpace(r.EmployeeRecognition) ? "A" : r.EmployeeRecognition),
            ("isactive", "Active", "yes"),

            ("ContactNo", "Contact No", r.ContactNo),
            ("Email", "Email", r.Email),
            ("PersentAdd", "Present Address", r.PresentAddress),
            ("PermanentAdd", "Permanent Address", r.PermanentAddress),
            ("emrg_no", "Emergency Contact No", r.EmergencyContactNo),
            ("emrg_add", "Emergency Contact / Address", r.EmergencyContactAddress),
            ("guardian", "Guardian", r.Guardian),

            ("BankCash", "Payment Mode", string.IsNullOrWhiteSpace(r.PaymentMode) ? "Bank" : r.PaymentMode),
            ("bank_name", "Bank Name", r.BankName),
            ("BankNo", "Bank Account No", r.BankAccountNo),
            ("IFSCCode", "IFSC Code", r.IfscCode),
            ("pan_no", "PAN No", r.PanNo),
            ("AadharNo", "Aadhaar No", r.AadhaarNo),
            ("UANNo", "UAN No", r.UanNo),
            ("PFaccount", "PF Account No", r.PfAccountNo),
            ("ESICNo", "ESIC No", r.EsicNo),
            ("nominee", "Nominee", r.Nominee),
            ("relatinship", "Nominee Relationship", r.NomineeRelationship),
            ("NomineeDOB", "Nominee DOB", r.NomineeDob),
        };

        using var connection = _database.CreatePayrollLoginEntryConnection();
        var schema = (await connection.QueryAsync<(string Column, string DataType, int? MaxLength)>(@"
SELECT COLUMN_NAME AS [Column], DATA_TYPE AS DataType, CHARACTER_MAXIMUM_LENGTH AS MaxLength
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'empinfo'", commandTimeout: 30))
            .ToDictionary(x => x.Column, StringComparer.OrdinalIgnoreCase);

        var columns = new List<string>();
        var parameters = new DynamicParameters();
        var errors = new List<string>();
        var i = 0;
        foreach (var (column, label, raw) in fields)
        {
            if (!schema.TryGetValue(column, out var col))
                continue;

            var value = ConvertValue(raw, col.DataType, col.MaxLength, label, errors);
            if (value is null)
                continue;

            var p = "@p" + i++;
            columns.Add($"[{col.Column}]");
            parameters.Add(p, value);
        }

        if (errors.Count > 0)
            throw new InvalidOperationException(string.Join(" ", errors));

        if (connection.State != System.Data.ConnectionState.Open)
            connection.Open();
        using var tx = connection.BeginTransaction();
        try
        {
            var exists = await connection.ExecuteScalarAsync<int>(@"
SELECT COUNT(*) FROM empinfo WITH (UPDLOCK, HOLDLOCK)
WHERE LTRIM(RTRIM(EmpCode)) = @EmpCode", new { EmpCode = empCode }, tx);
            if (exists > 0)
                throw new InvalidOperationException($"Employee Code {empCode} already exists in ERP. Use the suggested next code.");

            var names = string.Join(", ", columns);
            var values = string.Join(", ", Enumerable.Range(0, columns.Count).Select(n => "@p" + n));
            await connection.ExecuteAsync($"INSERT INTO empinfo ({names}) VALUES ({values})", parameters, tx, commandTimeout: 30);
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

        return new HrCreateEmployeeResultDto
        {
            EmpCode = empCode,
            Name = (r.Name ?? "").Trim(),
            CompanyName = (r.CompanyName ?? "").Trim(),
            Branch = (r.Branch ?? "").Trim(),
            IsConsultant = HrEmployeeRules.IsConsultant(r.CompanyName),
            CreatedBy = createdBy,
            Message = $"Employee {empCode} — {(r.Name ?? "").Trim()} added to ERP.",
        };
    }

    private static object? ConvertValue(object? raw, string dataType, int? maxLength, string label, List<string> errors)
    {
        if (raw is null)
            return null;
        if (raw is int or long or decimal or double)
            return raw;

        var s = raw.ToString()?.Trim() ?? "";
        if (s.Length == 0)
            return null;

        switch (dataType.ToLowerInvariant())
        {
            case "varchar":
            case "nvarchar":
            case "char":
            case "nchar":
                if (maxLength is > 0 && s.Length > maxLength)
                {
                    errors.Add($"{label} is too long (max {maxLength} characters).");
                    return null;
                }
                return s;
            case "datetime":
            case "date":
            case "smalldatetime":
            case "datetime2":
                if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
                    return d;
                errors.Add($"{label} must be a valid date.");
                return null;
            case "int":
            case "bigint":
            case "smallint":
            case "tinyint":
                if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                    return n;
                errors.Add($"{label} must be a whole number.");
                return null;
            case "numeric":
            case "decimal":
            case "float":
            case "real":
            case "money":
                if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var m))
                    return m;
                errors.Add($"{label} must be a number.");
                return null;
            case "bit":
                return s is "1" or "true" or "True" or "yes";
            default:
                return s;
        }
    }
}

public sealed class HrCompanyBranchDto
{
    public string CompanyName { get; set; } = "";
    public string Branch { get; set; } = "";
    public int Employees { get; set; }
}

public sealed class HrEmployeeFormOptionsDto
{
    public List<HrCompanyBranchDto> CompanyBranches { get; set; } = [];
    public List<string> Departments { get; set; } = [];
    public List<string> SubDepartments { get; set; } = [];
    public List<string> Designations { get; set; } = [];
    public List<string> WorkRoles { get; set; } = [];
    public List<string> Categories { get; set; } = [];
    public List<string> JobDescriptions { get; set; } = [];
}

public sealed class HrNextEmpCodeDto
{
    public string Branch { get; set; } = "";
    public string? Prefix { get; set; }
    public string? LastEmpCode { get; set; }
    public string? SuggestedEmpCode { get; set; }
    public List<HrEmpCodeSeriesDto> Alternatives { get; set; } = [];
}

public sealed class HrEmpCodeSeriesDto
{
    public string LastEmpCode { get; set; } = "";
    public string NextEmpCode { get; set; } = "";
}

public sealed class HrCreateEmployeeResultDto
{
    public string EmpCode { get; set; } = "";
    public string Name { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string Branch { get; set; } = "";
    public bool IsConsultant { get; set; }
    public string? CreatedBy { get; set; }
    public string Message { get; set; } = "";
}

/// <summary>Mirrors the ERP Employee Information form (Personal / Official / Communication / Bank-Tax).</summary>
public sealed class HrCreateEmployeeRequest
{
    public string? Username { get; set; }

    // Header
    public string? CompanyName { get; set; }
    public string? Branch { get; set; }
    public string? EmpCode { get; set; }
    public string? Name { get; set; }

    // Personal information
    public string? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? MaritalStatus { get; set; }
    public string? Language { get; set; }
    public string? Religion { get; set; }
    public string? Caste { get; set; }
    public string? Nationality { get; set; }
    public bool? Handicapped { get; set; }
    public string? BloodGroup { get; set; }
    public string? PassportNo { get; set; }
    public string? PassportValidFrom { get; set; }
    public string? PassportValidTo { get; set; }
    public string? CardId { get; set; }
    public string? JobDescription { get; set; }
    public bool? IsDirector { get; set; }
    public string? FatherName { get; set; }
    public string? MotherName { get; set; }
    public string? SpouseName { get; set; }
    public string? NoOfChildren { get; set; }
    public string? Qualification { get; set; }
    public string? Specialization { get; set; }
    public string? University { get; set; }
    public string? YearOfPassing { get; set; }
    public string? Skills { get; set; }
    public string? Abilities { get; set; }

    // Official records
    public string? DateOfJoining { get; set; }
    public string? ConfirmationDate { get; set; }
    public string? Designation { get; set; }
    public string? Department { get; set; }
    public string? SubDepartment { get; set; }
    public string? Category { get; set; }
    public string? SubCategory { get; set; }
    public string? WorkRole { get; set; }
    public string? WorkArea { get; set; }
    public string? WorkLocation { get; set; }
    public string? Experience { get; set; }
    public string? PreviousEmployer { get; set; }
    public string? ContractorName { get; set; }
    public bool? IsHoEmp { get; set; }
    public string? Ctc { get; set; }
    public bool? IsSalaryPerDay { get; set; }
    public string? SalaryPerDay { get; set; }
    public bool? PfApplicable { get; set; }
    public string? PfApplicableDate { get; set; }
    public bool? BonusApplicable { get; set; }
    public bool? HraApplicable { get; set; }
    public bool? AttendanceBonus { get; set; }
    public bool? Overtime { get; set; }
    public bool? Rotation { get; set; }
    public string? EmployeeRecognition { get; set; }

    // Communication details
    public string? ContactNo { get; set; }
    public string? Email { get; set; }
    public string? PresentAddress { get; set; }
    public string? PermanentAddress { get; set; }
    public string? EmergencyContactNo { get; set; }
    public string? EmergencyContactAddress { get; set; }
    public string? Guardian { get; set; }

    // Bank / tax details
    public string? PaymentMode { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNo { get; set; }
    public string? IfscCode { get; set; }
    public string? PanNo { get; set; }
    public string? AadhaarNo { get; set; }
    public string? UanNo { get; set; }
    public string? PfAccountNo { get; set; }
    public string? EsicNo { get; set; }
    public string? Nominee { get; set; }
    public string? NomineeRelationship { get; set; }
    public string? NomineeDob { get; set; }
}
