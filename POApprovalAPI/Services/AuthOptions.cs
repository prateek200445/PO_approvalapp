namespace POApprovalAPI.Services;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>
    /// When true, login also tries payroll LoginRights on port 3445 after portal auth fails.
    /// Keep false on Render — that host cannot reach 3445 and every miss becomes a multi-second hang.
    /// </summary>
    public bool EnablePayrollLoginFallback { get; set; } = false;
}
