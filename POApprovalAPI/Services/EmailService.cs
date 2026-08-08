using System.Net;
using System.Net.Mail;

namespace POApprovalAPI.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Queues email send in the background so approve/reject APIs are not blocked by SMTP.
    /// </summary>
    public Task SendMail(string to, string subject, string body)
    {
        _ = SendMailCoreAsync(to, subject, body);
        return Task.CompletedTask;
    }

    private async Task SendMailCoreAsync(string to, string subject, string body)
    {
        try
        {
            var host = _configuration["EmailSettings:Host"];
            var portText = _configuration["EmailSettings:Port"];
            var username = _configuration["EmailSettings:Username"];
            var password = _configuration["EmailSettings:Password"];

            if (string.IsNullOrWhiteSpace(host) ||
                string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(to))
            {
                Console.WriteLine("EMAIL SKIPPED: missing host/username/to");
                return;
            }

            // Password not configured — fail fast instead of hanging on SMTP auth
            if (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("EMAIL SKIPPED: EmailSettings:Password is not configured");
                return;
            }

            if (!int.TryParse(portText, out var port))
                port = 587;

            Console.WriteLine("========== EMAIL DEBUG ==========");
            Console.WriteLine($"SMTP Host : {host}");
            Console.WriteLine($"SMTP Port : {port}");
            Console.WriteLine($"From      : {username}");
            Console.WriteLine($"To        : {to}");
            Console.WriteLine($"Subject   : {subject}");
            Console.WriteLine("=================================");

            using var client = new SmtpClient(host)
            {
                Port = port,
                EnableSsl = true,
                Timeout = 8000, // don't hang approve flows
                Credentials = new NetworkCredential(username, password)
            };

            using var message = new MailMessage(username, to, subject, body);

            Console.WriteLine("STARTING EMAIL SEND...");
            await client.SendMailAsync(message);
            Console.WriteLine("EMAIL SENT SUCCESSFULLY");
        }
        catch (Exception ex)
        {
            Console.WriteLine("EMAIL ERROR (non-fatal):");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }
    }
}
