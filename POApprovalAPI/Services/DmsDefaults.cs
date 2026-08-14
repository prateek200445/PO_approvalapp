namespace POApprovalAPI.Services;

/// <summary>
/// Built-in ERP DMS settings so production works without manual env/config on the host.
/// Values match dmsService Web.config and ERP app.config endpoints.
/// </summary>
internal static class DmsDefaults
{
    internal const string FileLocation = @"D:\ERP Projects\dmsService\dmsService\Data\";

    internal static readonly string[] ServiceUrls =
    [
        "http://180.211.107.118/NEWDMSService/DMSService.svc",
        "http://180.211.107.118/DMSService/DMSService.svc",
        "http://103.240.33.122/NEWDMSService/DMSService.svc",
        "http://103.240.33.122/DMSService/DMSService.svc",
        "http://desktop-ijn98i2/DMSService/DMSService.svc",
    ];
}
