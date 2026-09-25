namespace POApprovalAPI.Services;

/// <summary>
/// Built-in credentials so production (Render) works without manual env setup.
/// Environment variables DB_PASSWORD / EMAIL_PASSWORD override these when set.
/// </summary>
internal static class AppSecretsDefaults
{
    internal const string DbPassword = "PlastOswal#@123$%^&*()iop";
    /// <summary>NEWERP payroll instance (MSSQLPAYROLL / port 3445) — live attendance &amp; empinfo.</summary>
    internal const string PayrollDbPassword = "Pil#Osl#Ahd@123$%^&*()iop";
    internal const string EmailPassword = "Mani$&22";
}
