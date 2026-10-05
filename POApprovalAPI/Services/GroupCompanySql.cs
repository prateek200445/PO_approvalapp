namespace POApprovalAPI.Services;

/// <summary>Group company of an ERP company name, from FactoryInfo.GroupName (same groups as the Sales dashboard).</summary>
public static class GroupCompanySql
{
    /// <summary>OUTER APPLY exposing <c>g.GroupName</c>; falls back to the company name when it has no group.</summary>
    public static string OuterApply(string companyColumn) => $@"
OUTER APPLY (
    SELECT COALESCE(
        (SELECT TOP 1 NULLIF(LTRIM(RTRIM(f.GroupName)), N'')
         FROM FactoryInfo f WITH (NOLOCK)
         WHERE LTRIM(RTRIM(f.Name)) = LTRIM(RTRIM({companyColumn}))
           AND NULLIF(LTRIM(RTRIM(f.GroupName)), N'') IS NOT NULL),
        NULLIF(LTRIM(RTRIM({companyColumn})), N'')) AS GroupName
) g";
}
