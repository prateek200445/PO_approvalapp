using System.Text.Json;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public static class RejectRequestHelper
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx",
    };

    public const long MaxAttachmentBytes = 10 * 1024 * 1024;

    public static async Task<string> ReadRemarksAsync(HttpRequest request)
    {
        if (request.HasFormContentType)
            return (request.Form["remarks"].ToString() ?? "").Trim();

        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var json = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(json))
            return "";

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.TryGetProperty("remarks", out var remarks))
            return (remarks.GetString() ?? "").Trim();

        return "";
    }

    public static IFormFile? GetOptionalAttachment(HttpRequest request) =>
        request.HasFormContentType ? request.Form.Files.GetFile("attachment") : null;

    public static async Task<(EmailAttachmentData? Attachment, string? Error)> ReadOptionalAttachmentAsync(
        IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return (null, null);

        if (file.Length > MaxAttachmentBytes)
            return (null, $"Attachment exceeds {MaxAttachmentBytes / (1024 * 1024)} MB limit.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(ext) || !AllowedExtensions.Contains(ext))
            return (null, "Attachment type not allowed. Use PDF, image, Word, or Excel.");

        await using var stream = file.OpenReadStream();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        return (new EmailAttachmentData(file.FileName, contentType, ms.ToArray()), null);
    }
}
