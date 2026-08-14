using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/health")]
public class HealthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public HealthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("email")]
    public async Task<IActionResult> EmailHealth()
    {
        var host = _configuration["EmailSettings:Host"] ?? "";
        var portText = _configuration["EmailSettings:Port"];
        var username = _configuration["EmailSettings:Username"] ?? "";
        var password = _configuration["EmailSettings:Password"] ?? "";
        var port = int.TryParse(portText, out var p) ? p : 587;

        var tcpOk = false;
        string? tcpError = null;
        try
        {
            using var client = new TcpClient();
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await client.ConnectAsync(host, port, cts.Token);
            tcpOk = client.Connected;
        }
        catch (Exception ex)
        {
            tcpError = ex.Message;
        }

        return Ok(new
        {
            smtpHost = host,
            smtpPort = port,
            smtpUsername = username,
            passwordConfigured = !string.IsNullOrWhiteSpace(password),
            tcpReachable = tcpOk,
            tcpError,
        });
    }
}
