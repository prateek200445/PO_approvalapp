using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;

namespace POApprovalAPI.Services;

/// <summary>
/// ERP MM → Display → Other → Next Week Pending Payment (<c>ERP.PendingPayment_n</c>).
/// List: <c>vw_Auto_Payment_Request_Data</c>. Punch: BillPaymentEntry + BillPaymentHODApproval.
/// Department names match Bill Payment Entry (<c>ERP.FrmBillPaymentEntr</c>) → <c>ItemDeptt.DepttName</c>.
/// </summary>
public class PendingPaymentService
{
    private const int QueryTimeoutSeconds = 300;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);
    private static readonly object PaymentNoLock = new();

    private readonly DatabaseService _database;
    private readonly IMemoryCache _cache;

    public PendingPaymentService(DatabaseService database, IMemoryCache cache)
    {
        _database = database;
        _cache = cache;
    }

    public async Task<PendingPaymentResultDto> GetAsync(string? company = null, bool refresh = false)
    {
        var companyKey = string.IsNullOrWhiteSpace(company) || IsAllCompanies(company)
            ? "all"
            : company.Trim();
        var cacheKey = $"pending-payment-next-week-v3|{companyKey.ToLowerInvariant()}";

        if (!refresh && _cache.TryGetValue(cacheKey, out PendingPaymentResultDto? cached) && cached is not null)
            return cached;

        var rows = await LoadRowsAsync(companyKey == "all" ? null : companyKey);
        var today = DateTime.Today;
        var result = new PendingPaymentResultDto
        {
            Items = rows,
            Count = rows.Count,
            TotalBillAmount = Math.Round(rows.Sum(r => r.BillAmount), 2),
            DateFrom = today.ToString("yyyy-MM-dd"),
            DateTo = today.AddDays(7).ToString("yyyy-MM-dd"),
            Source = "vw_Auto_Payment_Request_Data (ERP.PendingPayment_n)",
            AsOf = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        };

        _cache.Set(cacheKey, result, CacheTtl);
        return result;
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync()
    {
        const string cacheKey = "pending-payment-companies-v1";
        if (_cache.TryGetValue(cacheKey, out List<string>? cached) && cached is { Count: > 0 })
            return cached;

        using var connection = _database.CreateConnection();
        var names = (await connection.QueryAsync<string>(@"
SELECT DISTINCT LTRIM(RTRIM(Name))
FROM FactoryInfo WITH (NOLOCK)
WHERE ISNULL(Name, '') <> ''
ORDER BY 1",
            commandTimeout: 60)).ToList();

        _cache.Set(cacheKey, names, TimeSpan.FromHours(6));
        return names;
    }

    /// <summary>
    /// ERP "Punch Payment Request" — creates PRQ rows for selected next-week lines.
    /// </summary>
    public async Task<PendingPaymentPunchResultDto> PunchAsync(
        IReadOnlyList<PendingPaymentPunchLineDto> lines,
        string loginName)
    {
        if (string.IsNullOrWhiteSpace(loginName))
            throw new ArgumentException("Login name is required to punch payment requests.");
        if (lines is null || lines.Count == 0)
            throw new ArgumentException("Select at least one row to punch.");

        var login = loginName.Trim();
        var created = new List<PendingPaymentPunchedDto>();
        var errors = new List<string>();

        using var connection = _database.CreateConnection();
        if (connection.State != ConnectionState.Open)
            await ((SqlConnection)connection).OpenAsync();

        foreach (var line in lines)
        {
            try
            {
                var punched = await PunchOneAsync(connection, line, login);
                created.Add(punched);
            }
            catch (Exception ex)
            {
                errors.Add($"{line.CompanyName} / {line.BillNo} / {line.MRNo}: {ex.Message}");
            }
        }

        InvalidateListCache();

        return new PendingPaymentPunchResultDto
        {
            Created = created,
            CreatedCount = created.Count,
            Errors = errors,
            Message = created.Count > 0
                ? $"Punched {created.Count} payment request(s)."
                : "No payment requests were created.",
        };
    }

    private async Task<PendingPaymentPunchedDto> PunchOneAsync(
        IDbConnection connection,
        PendingPaymentPunchLineDto line,
        string login)
    {
        var company = (line.CompanyName ?? "").Trim();
        var party = (line.PartyName ?? "").Trim();
        var billNo = (line.BillNo ?? "").Trim();
        var mrNo = (line.MRNo ?? "").Trim();
        var year = (line.Year ?? "").Trim();
        var terms = (line.PaymentTerms ?? "").Trim();
        var amount = Math.Round(line.BillAmount, 2);

        if (company.Length == 0 || party.Length == 0)
            throw new InvalidOperationException("Company and Party are required.");
        if (amount <= 0)
            throw new InvalidOperationException("Bill amount must be greater than zero.");
        if (year.Length == 0)
            throw new InvalidOperationException("Year is required.");

        // Already punched fully for this bill+MRN?
        var already = await connection.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM BillPaymentEntry WITH (NOLOCK)
WHERE CompanyName = @Company
  AND VendorName = @Party
  AND Yr = @Year
  AND ISNULL(BillNo,'') = @BillNo
  AND ISNULL(MRNno,'') = @MRNo
  AND ISNULL(IsCancel,'No') = 'No'
  AND ISNULL(status,'') <> 'Rejected'",
            new { Company = company, Party = party, Year = year, BillNo = billNo, MRNo = mrNo },
            commandTimeout: 60);
        if (already > 0)
            throw new InvalidOperationException("A payment request already exists for this bill/MRN.");

        var indent = await connection.ExecuteScalarAsync<string>(@"
SELECT TOP 1 LTRIM(RTRIM(ISNULL(IndentCode, '')))
FROM FactoryInfo WITH (NOLOCK)
WHERE LTRIM(RTRIM(Name)) = @Company",
            new { Company = company },
            commandTimeout: 30);
        if (string.IsNullOrWhiteSpace(indent))
            throw new InvalidOperationException($"No IndentCode found in FactoryInfo for {company}.");

        // Bill Payment Entry Department dropdown = ItemDeptt.DepttName
        var deptt = await ResolveBillPaymentDepartmentAsync(
            connection, line.Deptt, company, mrNo, transaction: null);

        var prefix = $"{indent}/PRQ/{year}";
        var (paymentNo, indx) = AllocatePaymentNo(connection, prefix);
        var today = DateTime.Today;
        var billDate = line.BillDate?.Date;
        var mrnDate = line.MRNDt?.Date;

        using var tx = connection.BeginTransaction();
        try
        {
            await connection.ExecuteAsync(@"
INSERT INTO BillPaymentEntry (
    PaymentNo, yr, CompanyName, VendorName, BillNo, BillDate, MRNno, MRNDate,
    BillAmount, PaymentTerms, PaymentAmount, PaymentDate, Loginname, Sysdate, Indx,
    status, Remarks, PriorityLevel, LC, UTRno, isPaid, IsCancel, PaidDate,
    Currency, CurrencyRate, TDS, DebitNoteAmnt, PaymentBankName, PaymentBankAccNo, SpeReq
) VALUES (
    @PaymentNo, @Yr, @CompanyName, @VendorName, @BillNo, @BillDate, @MRNno, @MRNDate,
    @BillAmount, @PaymentTerms, @PaymentAmount, @PaymentDate, @Loginname, @Sysdate, @Indx,
    'Pending', '', 'Low', 'No', '', 'no', 'no', '1900-01-01',
    'Rs.', 1, 0, 0, '', '', 0
)",
                new
                {
                    PaymentNo = paymentNo,
                    Yr = year,
                    CompanyName = company,
                    VendorName = party,
                    BillNo = billNo,
                    BillDate = (object?)billDate ?? DBNull.Value,
                    MRNno = mrNo,
                    MRNDate = (object?)mrnDate ?? DBNull.Value,
                    BillAmount = amount,
                    PaymentTerms = terms,
                    PaymentAmount = amount,
                    PaymentDate = today,
                    Loginname = login,
                    Sysdate = today,
                    Indx = indx,
                },
                tx,
                commandTimeout: 60);

            try
            {
                await connection.ExecuteAsync(
                    "INSERT INTO TempSavePaymentNo (PaymentNo) VALUES (@PaymentNo)",
                    new { PaymentNo = paymentNo },
                    tx,
                    commandTimeout: 30);
            }
            catch
            {
                // duplicate in temp table is non-fatal
            }

            // Next Week punch is SpeReq=0: prefer non-HOD allocation rows matched by
            // Bill Payment Entry department name; fall back to HOD then company-wide.
            var approvers = await LoadApproversAsync(connection, company, deptt, hodOnly: false, tx);
            if (approvers.Count == 0)
                approvers = await LoadApproversAsync(connection, company, deptt, hodOnly: true, tx);
            if (approvers.Count == 0)
                approvers = await LoadApproversAsync(connection, company, deptt: "", hodOnly: false, tx);
            if (approvers.Count == 0)
                approvers = await LoadApproversAsync(connection, company, deptt: "", hodOnly: true, tx);

            if (approvers.Count == 0)
                throw new InvalidOperationException($"No payment approvers configured for {company}.");

            foreach (var approver in approvers)
            {
                await connection.ExecuteAsync(@"
INSERT INTO BillPaymentHODApproval (
    BillNo, MRNo, PartyName, PaymentAmount, PaymentDate, paymentNo,
    LoginName, ApprovalName, Status, ApprovalDate, Comment
) VALUES (
    @BillNo, @MRNo, @PartyName, @PaymentAmount, @PaymentDate, @PaymentNo,
    @LoginName, @ApprovalName, 'Pending', NULL, ''
)",
                    new
                    {
                        BillNo = billNo,
                        MRNo = mrNo,
                        PartyName = party,
                        PaymentAmount = amount,
                        PaymentDate = today,
                        PaymentNo = paymentNo,
                        LoginName = login,
                        ApprovalName = approver,
                    },
                    tx,
                    commandTimeout: 30);
            }

            tx.Commit();
            return new PendingPaymentPunchedDto
            {
                PaymentNo = paymentNo,
                CompanyName = company,
                PartyName = party,
                BillNo = billNo,
                MRNo = mrNo,
                PaymentAmount = amount,
                ApproverCount = approvers.Count,
            };
        }
        catch
        {
            try { tx.Rollback(); } catch { /* ignore */ }
            throw;
        }
    }

    private static (string PaymentNo, int Indx) AllocatePaymentNo(IDbConnection connection, string prefix)
    {
        lock (PaymentNoLock)
        {
            var like = prefix + "/%";
            var maxEntry = connection.ExecuteScalar<int?>(@"
SELECT MAX(TRY_CAST(RIGHT(PaymentNo, CHARINDEX('/', REVERSE(PaymentNo)) - 1) AS int))
FROM BillPaymentEntry WITH (UPDLOCK, HOLDLOCK)
WHERE PaymentNo LIKE @Like",
                new { Like = like },
                commandTimeout: 60) ?? 0;

            var maxTemp = connection.ExecuteScalar<int?>(@"
SELECT MAX(TRY_CAST(RIGHT(PaymentNo, CHARINDEX('/', REVERSE(PaymentNo)) - 1) AS int))
FROM TempSavePaymentNo WITH (UPDLOCK, HOLDLOCK)
WHERE PaymentNo LIKE @Like",
                new { Like = like },
                commandTimeout: 60) ?? 0;

            var next = Math.Max(maxEntry, maxTemp) + 1;
            return ($"{prefix}/{next}", next);
        }
    }

    private void InvalidateListCache()
    {
        // Callers pass refresh=true after punch; cache entries expire via TTL.
    }

    private async Task<List<PendingPaymentRowDto>> LoadRowsAsync(string? company)
    {
        using var connection = _database.CreateConnection();

        // ItemDeptt is tiny — load once and map in memory (avoids per-row EXISTS on the heavy view).
        var billPaymentDepts = new HashSet<string>(
            (await connection.QueryAsync<string>(@"
SELECT LTRIM(RTRIM(DepttName))
FROM ItemDeptt WITH (NOLOCK)
WHERE ISNULL(DepttName, '') <> ''",
                commandTimeout: 30)).Select(d => d.Trim()),
            StringComparer.OrdinalIgnoreCase);

        // Keep SQL simple: ERP view is already expensive; do not add correlated subqueries.
        var sql = @"
SELECT
    LTRIM(RTRIM(ISNULL(v.CompanyName, ''))) AS CompanyName,
    LTRIM(RTRIM(ISNULL(v.PartyName, ''))) AS PartyName,
    LTRIM(RTRIM(ISNULL(v.BillNo, ''))) AS BillNo,
    v.BillDate,
    LTRIM(RTRIM(ISNULL(v.MRNo, ''))) AS MRNo,
    v.MRNDt,
    LTRIM(RTRIM(ISNULL(v.[Year], ''))) AS [Year],
    CAST(ISNULL(v.BillAMount, 0) AS float) AS BillAmount,
    LTRIM(RTRIM(ISNULL(v.PaymentTerms, ''))) AS PaymentTerms,
    v.DueDt,
    LTRIM(RTRIM(ISNULL(v.Deptt, ''))) AS Deptt
FROM vw_Auto_Payment_Request_Data v WITH (NOLOCK)
WHERE (@Company IS NULL OR LTRIM(RTRIM(v.CompanyName)) = @Company)
ORDER BY v.DueDt, v.CompanyName, v.PartyName, v.BillNo";

        var rows = await connection.QueryAsync<PendingPaymentRowDto>(
            sql,
            new { Company = company },
            commandTimeout: QueryTimeoutSeconds);

        return rows.Select(r =>
        {
            r.BillDate = TruncateDate(r.BillDate);
            r.MRNDt = TruncateDate(r.MRNDt);
            r.DueDt = TruncateDate(r.DueDt);
            r.BillAmount = Math.Round(r.BillAmount, 2);
            var dept = (r.Deptt ?? "").Trim();
            r.Deptt = dept.Length > 0 && billPaymentDepts.Contains(dept) ? dept : "";
            return r;
        }).ToList();
    }

    /// <summary>
    /// Bill Payment Entry Department combo is filled from ItemDeptt.DepttName.
    /// Prefer the incoming name when it is a master dept; else resolve from MRN item dept.
    /// </summary>
    private static async Task<string> ResolveBillPaymentDepartmentAsync(
        IDbConnection connection,
        string? candidate,
        string company,
        string mrNo,
        IDbTransaction? transaction)
    {
        var raw = (candidate ?? "").Trim();
        if (raw.Length > 0)
        {
            var matched = await connection.ExecuteScalarAsync<string>(@"
SELECT TOP 1 LTRIM(RTRIM(DepttName))
FROM ItemDeptt WITH (NOLOCK)
WHERE LTRIM(RTRIM(DepttName)) = @Deptt",
                new { Deptt = raw },
                transaction,
                commandTimeout: 30);
            if (!string.IsNullOrWhiteSpace(matched))
                return matched.Trim();
        }

        if (company.Length == 0 || mrNo.Length == 0)
            return "";

        var fromMrn = await connection.ExecuteScalarAsync<string>(@"
SELECT TOP 1 LTRIM(RTRIM(d.DepttName))
FROM Vw_StoreInwards si WITH (NOLOCK)
INNER JOIN ItemDeptt d WITH (NOLOCK)
  ON LTRIM(RTRIM(d.DepttName)) = LTRIM(RTRIM(ISNULL(si.itemDeptt, si.Deptt)))
WHERE si.MRNo = @MRNo
  AND LTRIM(RTRIM(si.CompanyName)) = @Company
GROUP BY LTRIM(RTRIM(d.DepttName))
ORDER BY COUNT(*) DESC",
            new { Company = company, MRNo = mrNo },
            transaction,
            commandTimeout: 60);

        return (fromMrn ?? "").Trim();
    }

    private static async Task<List<string>> LoadApproversAsync(
        IDbConnection connection,
        string company,
        string deptt,
        bool hodOnly,
        IDbTransaction? transaction)
    {
        var sql = @"
SELECT DISTINCT LTRIM(RTRIM(ApprovalName))
FROM BillPaymentApprovalAllocation WITH (NOLOCK)
WHERE ISNULL(ApprovalName, '') <> ''
  AND (
        LTRIM(RTRIM(CompanyName)) = @Company
     OR LOWER(LTRIM(RTRIM(CompanyName))) = 'all'
  )
  AND (
        (@HodOnly = 1 AND LOWER(ISNULL(ishod, '')) = 'yes')
     OR (@HodOnly = 0 AND LOWER(ISNULL(ishod, '')) <> 'yes')
  )
  AND (
        @Deptt = ''
     OR LTRIM(RTRIM(Deptt)) = '*'
     OR Deptt LIKE '%' + @Deptt + '%'
  )
ORDER BY 1";

        return (await connection.QueryAsync<string>(
            sql,
            new { Company = company, Deptt = deptt, HodOnly = hodOnly ? 1 : 0 },
            transaction,
            commandTimeout: 60)).ToList();
    }

    private static bool IsAllCompanies(string? company) =>
        string.IsNullOrWhiteSpace(company)
        || company.Trim().Equals("All Companies", StringComparison.OrdinalIgnoreCase);

    private static DateTime? TruncateDate(DateTime? value) =>
        value.HasValue ? value.Value.Date : null;
}

public class PendingPaymentResultDto
{
    public List<PendingPaymentRowDto> Items { get; set; } = new();
    public int Count { get; set; }
    public double TotalBillAmount { get; set; }
    public string DateFrom { get; set; } = "";
    public string DateTo { get; set; } = "";
    public string Source { get; set; } = "";
    public string AsOf { get; set; } = "";
}

public class PendingPaymentRowDto
{
    public string CompanyName { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string BillNo { get; set; } = "";
    public DateTime? BillDate { get; set; }
    public string MRNo { get; set; } = "";
    public DateTime? MRNDt { get; set; }
    public string Year { get; set; } = "";
    public double BillAmount { get; set; }
    public string PaymentTerms { get; set; } = "";
    public DateTime? DueDt { get; set; }
    public string Deptt { get; set; } = "";
}

public class PendingPaymentPunchLineDto
{
    public string CompanyName { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string BillNo { get; set; } = "";
    public DateTime? BillDate { get; set; }
    public string MRNo { get; set; } = "";
    public DateTime? MRNDt { get; set; }
    public string Year { get; set; } = "";
    public double BillAmount { get; set; }
    public string PaymentTerms { get; set; } = "";
    public string Deptt { get; set; } = "";
}

public class PendingPaymentPunchedDto
{
    public string PaymentNo { get; set; } = "";
    public string CompanyName { get; set; } = "";
    public string PartyName { get; set; } = "";
    public string BillNo { get; set; } = "";
    public string MRNo { get; set; } = "";
    public double PaymentAmount { get; set; }
    public int ApproverCount { get; set; }
}

public class PendingPaymentPunchResultDto
{
    public List<PendingPaymentPunchedDto> Created { get; set; } = new();
    public int CreatedCount { get; set; }
    public List<string> Errors { get; set; } = new();
    public string Message { get; set; } = "";
}
