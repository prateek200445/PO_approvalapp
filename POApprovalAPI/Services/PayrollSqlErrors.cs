using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace POApprovalAPI.Services;

public static class PayrollSqlErrors
{
    public const string UnreachableMessage =
        "Payroll database (port 3445) is not reachable from the cloud API. " +
        "HR attendance/leave needs that server. Ask IT to allow outbound TCP from the Render host to " +
        "103.240.33.122:3445 (same server as portal SQL 5115, or run the API on the company network). " +
        "Login can still work via portal SQL on 5115.";

    public static bool IsUnreachable(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException!)
        {
            if (e is SqlException sql)
            {
                // Network / timeout / server not found style errors
                if (sql.Number is 53 or 40 or -2 or 10054 or 10060 or 10061)
                    return true;
                if (sql.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                    sql.Message.Contains("not found or was not accessible", StringComparison.OrdinalIgnoreCase) ||
                    sql.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            if (e is TimeoutException)
                return true;

            if (e is DbException db &&
                (db.Message.Contains("network-related", StringComparison.OrdinalIgnoreCase) ||
                 db.Message.Contains("not found or was not accessible", StringComparison.OrdinalIgnoreCase) ||
                 db.Message.Contains("TCP Provider", StringComparison.OrdinalIgnoreCase)))
                return true;
        }

        return false;
    }

    public static string UserMessage(Exception ex) =>
        IsUnreachable(ex) ? UnreachableMessage : ex.Message;
}
