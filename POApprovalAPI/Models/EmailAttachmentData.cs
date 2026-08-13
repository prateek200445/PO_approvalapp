namespace POApprovalAPI.Models;

public sealed class EmailAttachmentData
{
    public EmailAttachmentData(string fileName, string? contentType, byte[] bytes)
    {
        FileName = fileName;
        ContentType = contentType;
        Bytes = bytes;
    }

    public string FileName { get; }
    public string? ContentType { get; }
    public byte[] Bytes { get; }
}
