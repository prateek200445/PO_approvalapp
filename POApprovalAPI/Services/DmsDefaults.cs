namespace POApprovalAPI.Services;

/// <summary>
/// Built-in ERP DMS settings so production works without manual env/config on the host.
/// Verified working endpoint (ERP app.config + live Render download test).
/// </summary>
internal static class DmsDefaults
{
    internal const string FileLocation = @"D:\ERP Projects\dmsService\dmsService\Data\";

    internal const string ServiceUrl = "http://180.211.107.118/NEWDMSService/DMSService.svc";
}
