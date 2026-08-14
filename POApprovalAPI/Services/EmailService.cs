using System.Net;
using System.Net.Mail;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Queues email in the background so approve/reject is not blocked by SMTP.
    /// </summary>
    public Task SendMail(
        string to,
        string subject,
        string body,
        IReadOnlyList<EmailAttachmentData>? attachments = null,
        string? cc = null,
        string? bcc = null)
    {
        return SendMailAsync(to, subject, body, attachments, wait: false, cc: cc, bcc: bcc);
    }

    /// <summary>
    /// Sends email and waits for SMTP completion (for user-initiated actions like BOM share).
    /// </summary>
    public Task SendMailAndWaitAsync(
        string to,
        string subject,
        string body,
        IReadOnlyList<EmailAttachmentData>? attachments = null,
        string? cc = null,
        string? bcc = null)
    {
        return SendMailAsync(to, subject, body, attachments, wait: true, cc: cc, bcc: bcc);
    }

    private Task SendMailAsync(
        string to,
        string subject,
        string body,
        IReadOnlyList<EmailAttachmentData>? attachments,
        bool wait,
        string? cc = null,
        string? bcc = null)
    {
        var clonedAttachments = attachments?
            .Select(a => new EmailAttachmentData(a.FileName, a.ContentType, a.Bytes.ToArray()))
            .ToList();

        if (wait)
            return SendMailCoreAsync(to, subject, body, clonedAttachments, cc, bcc, throwOnError: true);

        _ = SendMailInBackgroundAsync(to, subject, body, clonedAttachments, cc, bcc);
        return Task.CompletedTask;
    }

    private async Task SendMailInBackgroundAsync(
        string to,
        string subject,
        string body,
        IReadOnlyList<EmailAttachmentData>? attachments,
        string? cc,
        string? bcc)
    {
        try
        {
            await SendMailCoreAsync(to, subject, body, attachments, cc, bcc, throwOnError: false);
        }
        catch (Exception ex)
        {
            Console.WriteLine("EMAIL ERROR (non-fatal):");
            Console.WriteLine(ex.Message);
        }
    }

    private async Task SendMailCoreAsync(
        string to,
        string subject,
        string body,
        IReadOnlyList<EmailAttachmentData>? attachments,
        string? cc = null,
        string? bcc = null,
        bool throwOnError = false)
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
                if (throwOnError)
                    throw new InvalidOperationException("Email is not configured or recipient is missing.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                Console.WriteLine("EMAIL SKIPPED: EmailSettings:Password is not configured");
                if (throwOnError)
                    throw new InvalidOperationException("Email password is not configured on the server.");
                return;
            }

            if (!int.TryParse(portText, out var port))
                port = 587;

            Console.WriteLine("========== EMAIL DEBUG ==========");
            Console.WriteLine($"SMTP Host : {host}");
            Console.WriteLine($"SMTP Port : {port}");
            Console.WriteLine($"From      : {username}");
            Console.WriteLine($"To        : {to}");
            if (!string.IsNullOrWhiteSpace(cc)) Console.WriteLine($"Cc        : {cc}");
            if (!string.IsNullOrWhiteSpace(bcc)) Console.WriteLine($"Bcc       : {bcc}");
            Console.WriteLine($"Subject   : {subject}");
            Console.WriteLine($"Attach    : {attachments?.Count ?? 0}");
            Console.WriteLine("=================================");

            using var client = new SmtpClient(host)
            {
                Port = port,
                EnableSsl = true,
                Timeout = 60000,
                Credentials = new NetworkCredential(username, password),
            };

            using var message = new MailMessage
            {
                From = new MailAddress(username),
                Subject = subject,
                Body = body,
            };

            AddAddresses(message.To, to);
            AddAddresses(message.CC, cc);
            AddAddresses(message.Bcc, bcc);

            if (message.To.Count == 0)
            {
                if (throwOnError)
                    throw new InvalidOperationException("At least one valid recipient email is required.");
                Console.WriteLine("EMAIL SKIPPED: no valid To addresses");
                return;
            }

            if (attachments != null)
            {
                foreach (var file in attachments)
                {
                    var stream = new MemoryStream(file.Bytes);
                    var attachment = new Attachment(stream, file.FileName);
                    if (!string.IsNullOrWhiteSpace(file.ContentType))
                        attachment.ContentType = new System.Net.Mime.ContentType(file.ContentType);
                    message.Attachments.Add(attachment);
                }
            }

            Console.WriteLine("STARTING EMAIL SEND...");
            await client.SendMailAsync(message);
            Console.WriteLine("EMAIL SENT SUCCESSFULLY");
        }
        catch (Exception ex)
        {
            Console.WriteLine("EMAIL ERROR:");
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            if (throwOnError)
                throw;
        }
    }

    private static void AddAddresses(MailAddressCollection collection, string? addresses)
    {
        if (string.IsNullOrWhiteSpace(addresses))
            return;

        foreach (var part in addresses.Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;
            collection.Add(part);
        }
    }
}
