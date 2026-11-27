namespace POApprovalAPI.Services;

/// <summary>GST portal logins by GSTIN, used when the credential workbook is not on the API host.</summary>
internal static class GstPortalCredentials
{
    public static readonly (string Gstin, string Login, string Password)[] All =
    [
        ("24AAACO3557B2ZV", "Oel_commodities", "July@2627"),
        ("24AABCG1282C2Z6", "gpl.1984", "Hpbl@2627"),
        ("24AAACO3087C1ZT", "plastene", "Plastene@2627"),
        ("24AAECK6307M1ZF", "Kp_woven", "Kpw@2526"),
        ("24AAFCP2956A1ZR", "Polyfilms", "Ppl@2627"),
        ("24AAACC7815N1ZI", "Hcpenterprise", "Hcp#2627"),
        ("24AFOPG7793M1ZO", "Jainbulkpack", "Jbp@2627"),
        ("24AACCE8416D1Z0", "Eliteinks", "Elite@2627"),
        ("24AADCM8286G1Z9", "Marudharp", "Marudhar@2627"),
        ("24AAACO7290H4ZB", "oswalextsez", "Oel#2627"),
        ("24AAECK6307M2ZE", "Kpwoven_sez", "Kpw@2026"),
        ("24AMRPR0664A1Z4", "viram_1987", "Viram@2627"),
        ("24AAVHA3983R1ZC", "Amitmehta1986", "Samkit@2627"),
    ];
}
