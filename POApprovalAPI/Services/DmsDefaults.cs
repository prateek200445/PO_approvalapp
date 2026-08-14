namespace POApprovalAPI.Services;

/// <summary>
/// Built-in ERP DMS settings so production works without manual env/config on the host.
/// Verified working endpoint (ERP app.config + live Render download test).
/// </summary>
internal static class DmsDefaults
{
    internal const string FileLocation = @"D:\ERP Projects\dmsService\dmsService\Data\";

    /// <summary>Primary DMS host (same network as live SQL server).</summary>
    internal const string ServiceUrl = "http://103.240.33.122/NEWDMSService/DMSService.svc";

    /// <summary>Legacy ERP DMS host from app.config.</summary>
    internal static readonly string[] ServiceUrls =
    [
        "http://180.211.107.118/NEWDMSService/DMSService.svc",
    ];
}
