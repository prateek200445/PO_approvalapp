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

    public async Task SendMail(string to, string subject, string body)
    {
        var host = _configuration["EmailSettings:Host"];
        var port = int.Parse(_configuration["EmailSettings:Port"]!);
        var username = _configuration["EmailSettings:Username"];
        var password = _configuration["EmailSettings:Password"];

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
            Credentials = new NetworkCredential(username, password)
        };

        var message = new MailMessage(username!, to, subject, body);

        try
        {
            Console.WriteLine("STARTING EMAIL SEND...");

            await client.SendMailAsync(message);

            Console.WriteLine("EMAIL SENT SUCCESSFULLY");
        }
        catch (Exception ex)
        {
            Console.WriteLine("EMAIL ERROR:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);

            throw;
        }
    }
}