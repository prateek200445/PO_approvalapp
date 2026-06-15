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

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        using var connection = _database.CreateConnection();

      var user = await connection.QueryFirstOrDefaultAsync(@"
    SELECT
        l.Name AS UserName,
        p.authority,
        p.Deptt
    FROM Loginentry.dbo.LoginRights l
    LEFT JOIN poallocation p
        ON l.Name = p.username
    WHERE l.Name = @UserName
      AND l.Password = @Password
",
new
{
    request.UserName,
    request.Password
});

        if (user == null)
        {
            return Unauthorized(new
            {
                username = request.UserName,
                password = request.Password,
                message = "User not found"
            });
        }

        return Ok(user);
    }
}

public class LoginRequest
{
    public string UserName { get; set; } = "";
    public string Password { get; set; } = "";
}