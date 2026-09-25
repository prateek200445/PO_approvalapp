using Dapper;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DatabaseService _database;

    public AuthController(DatabaseService database)
    {
        _database = database;
    }

    /// <summary>
    /// Login credentials: portal LoginRights (5115) first, then payroll LoginRights (3445).
    /// Authority / Deptt always come from MaterialProcessing poallocation on 5115.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var userName = (request.UserName ?? "").Trim();
        var password = request.Password ?? "";
        if (userName.Length == 0)
            return Unauthorized(new { message = "Invalid Username or Password" });

        string? empCode = null;
        string? fullName = null;
        var authenticated = false;

        // 1) Portal auth (5115)
        try
        {
            using var portal = _database.CreateConnection();
            var portalUser = await portal.QueryFirstOrDefaultAsync<LoginRow>(@"
SELECT
    LTRIM(RTRIM(l.Name)) AS UserName,
    NULLIF(LTRIM(RTRIM(l.EmpCode)), '') AS EmpCode,
    NULLIF(LTRIM(RTRIM(ISNULL(l.FullName, l.Name))), '') AS FullName
FROM Loginentry.dbo.LoginRights l
WHERE LTRIM(RTRIM(l.Name)) = @UserName
  AND l.Password = @Password",
                new { UserName = userName, Password = password });

            if (portalUser != null)
            {
                authenticated = true;
                empCode = portalUser.EmpCode;
                fullName = portalUser.FullName;
            }
        }
        catch
        {
            // Fall through to payroll
        }

        // 2) Payroll LoginRights (3445) — HR accounts that only exist there
        if (!authenticated)
        {
            try
            {
                using var payroll = _database.CreatePayrollLoginEntryConnection();
                var payUser = await payroll.QueryFirstOrDefaultAsync<LoginRow>(@"
SELECT
    LTRIM(RTRIM(Name)) AS UserName,
    NULLIF(LTRIM(RTRIM(EmpCode)), '') AS EmpCode,
    NULLIF(LTRIM(RTRIM(ISNULL(FullName, Name))), '') AS FullName
FROM LoginRights WITH (NOLOCK)
WHERE LTRIM(RTRIM(Name)) = @UserName
  AND Password = @Password",
                    new { UserName = userName, Password = password });

                if (payUser != null)
                {
                    authenticated = true;
                    empCode = payUser.EmpCode;
                    fullName = payUser.FullName;
                }
            }
            catch
            {
                // Keep unauthenticated
            }
        }

        if (!authenticated)
            return Unauthorized(new { message = "Invalid Username or Password" });

        // Rest from 5115: PO authority / department
        string? authority = null;
        string? deptt = null;
        try
        {
            using var app = _database.CreateConnection();
            var alloc = await app.QueryFirstOrDefaultAsync<(string? authority, string? Deptt)>(@"
SELECT TOP 1 authority, Deptt
FROM poallocation WITH (NOLOCK)
WHERE LTRIM(RTRIM(username)) = @UserName",
                new { UserName = userName });
            authority = alloc.authority;
            deptt = alloc.Deptt;
        }
        catch
        {
            // Optional — many HR logins have no poallocation row
        }

        return Ok(new
        {
            UserName = userName,
            EmpCode = empCode,
            FullName = fullName,
            authority,
            Deptt = deptt,
        });
    }

    private sealed class LoginRow
    {
        public string? UserName { get; set; }
        public string? EmpCode { get; set; }
        public string? FullName { get; set; }
    }
}

public class LoginRequest
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}
