namespace POApprovalAPI.Models;

using System.Text.Json.Serialization;

public sealed class GstPortalStartRequest
{
    public string CompanyName { get; set; } = "";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal AmountTolerance { get; set; }
    public string Username { get; set; } = "";
    public bool ExploreOnly { get; set; }
}

public sealed class GstPortalCaptchaRequest
{
    public string SessionId { get; set; } = "";
    public string Captcha { get; set; } = "";
    public string Username { get; set; } = "";
}

public sealed class GstPortalOtpRequest
{
    public string SessionId { get; set; } = "";
    public string Otp { get; set; } = "";
    public string Username { get; set; } = "";
}

public sealed class GstPortalStep
{
    public string SessionId { get; set; } = "";
    public string Status { get; set; } = "";
    public string Gstin { get; set; } = "";
    public string CaptchaImage { get; set; } = "";
    public string Message { get; set; } = "";
    [JsonIgnore]
    public List<byte[]> Workbooks { get; set; } = [];
    public string CompanyName { get; set; } = "";
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public decimal AmountTolerance { get; set; }
}

public sealed class GstPortalDiagnostic
{
    public string SessionId { get; set; } = "";
    public string Url { get; set; } = "";
    public string Title { get; set; } = "";
    public int PageCount { get; set; }
    public List<string> Stages { get; set; } = [];
    public List<string> VisibleLinks { get; set; } = [];
    public List<string> Selects { get; set; } = [];
    public string BodyText { get; set; } = "";
}
