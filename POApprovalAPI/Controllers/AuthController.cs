using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly DatabaseService _database;
    private readonly AuthOptions _options;

    public AuthController(DatabaseService database, IOptions<AuthOptions> options)
    {
        _database = database;
        _options = options.Value;
    }

    /// <summary>
    /// Login against portal LoginRights (5115). Optional payroll (3445) fallback is off by default.
    /// Authority / Deptt come from poallocation on 5115 in the same round-trip.
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
        object? authority = null;
        string? deptt = null;
        var authenticated = false;

        // Single round-trip: credentials + PO authority from 5115
        try
        {
            using var portal = _database.CreateConnection();
            var portalUser = await portal.QueryFirstOrDefaultAsync<LoginRow>(@"
SELECT
    LTRIM(RTRIM(l.Name)) AS UserName,
    NULLIF(LTRIM(RTRIM(l.EmpCode)), '') AS EmpCode,
    NULLIF(LTRIM(RTRIM(ISNULL(l.FullName, l.Name))), '') AS FullName,
    p.authority AS Authority,
    p.Deptt AS Deptt
FROM Loginentry.dbo.LoginRights l
LEFT JOIN poallocation p
    ON LTRIM(RTRIM(p.username)) = LTRIM(RTRIM(l.Name))
WHERE LTRIM(RTRIM(l.Name)) = @UserName
  AND l.Password = @Password",
                new { UserName = userName, Password = password });

            if (portalUser != null)
            {
                authenticated = true;
                empCode = portalUser.EmpCode;
                fullName = portalUser.FullName;
                authority = portalUser.Authority;
                deptt = portalUser.Deptt;
            }
        }
        catch
        {
            // Fall through to optional payroll
        }

        // Optional payroll LoginRights (3445) — disabled on production by default
        if (!authenticated && _options.EnablePayrollLoginFallback)
        {
            try
            {
                using var payroll = _database.CreatePayrollLoginEntryConnection();
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                await payroll.OpenAsync(cts.Token);
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
        public object? Authority { get; set; }
        public string? Deptt { get; set; }
    }
}

public class LoginRequest
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}
