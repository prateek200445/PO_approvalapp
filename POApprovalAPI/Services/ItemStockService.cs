using System.Data;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class ItemStockService
{
    private const int TimeoutSeconds = 180;
    private readonly DatabaseService _database;

    public ItemStockService(DatabaseService database)
    {
        _database = database;
    }

    public async Task<IReadOnlyList<string>> GetCompaniesAsync(CancellationToken ct = default)
    {
        await using var connection = _database.CreateConnection();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"
SELECT Name
FROM FactoryInfo WITH (NOLOCK)
WHERE ISNULL(Name, '') <> ''
ORDER BY Name";
        var names = new List<string>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            names.Add(reader.GetString(0).Trim());
        return names;
    }

    public async Task<ItemStockResult> QueryAsync(ItemStockQueryRequest request, CancellationToken ct = default)
    {
        var company = (request.CompanyName ?? "").Trim();
        var item = (request.ItemCode ?? "").Trim();
        if (company.Length == 0)
            throw new ArgumentException("Company is required.");
        if (item.Length == 0)
            throw new ArgumentException("Item code is required.");
        if (request.DateTo.Date < request.DateFrom.Date)
            throw new ArgumentException("Date to is before date from.");

        var result = new ItemStockResult
        {
            CompanyName = company,
            ItemCode = item,
            DateFrom = request.DateFrom.Date,
            DateTo = request.DateTo.Date,
        };

        await using var connection = _database.CreateConnection();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "dbo.usp_ItemStockSummary";
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandTimeout = TimeoutSeconds;
            cmd.Parameters.Add("@CompanyName", SqlDbType.VarChar, 150).Value = company;
            cmd.Parameters.Add("@ItemCode", SqlDbType.VarChar, 50).Value = item;
            cmd.Parameters.Add("@DateFrom", SqlDbType.Date).Value = request.DateFrom.Date;
            cmd.Parameters.Add("@DateTo", SqlDbType.Date).Value = request.DateTo.Date;

            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (string.IsNullOrEmpty(result.ItemName))
                    result.ItemName = Str(reader, "ItemName");
                result.Summary.Add(new ItemStockSummaryLine
                {
                    LineType = Str(reader, "LineType"),
                    MovementType = Str(reader, "MovementType"),
                    InwardQty = Dec(reader, "InwardQty"),
                    OutwardQty = Dec(reader, "OutwardQty"),
                    Balance = Dec(reader, "Balance"),
                });
            }

            if (await reader.NextResultAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    result.Transactions.Add(new ItemStockTxnLine
                    {
                        TxnDate = reader.GetDateTime(reader.GetOrdinal("TxnDate")),
                        MovementType = Str(reader, "MovementType"),
                        DocNo = Str(reader, "DocNo"),
                        InwardQty = Dec(reader, "InwardQty"),
                        OutwardQty = Dec(reader, "OutwardQty"),
                        Balance = Dec(reader, "Balance"),
                    });
                }
            }
        }

        return result;
    }

    public async Task<ItemStockResult> QueryRollsAsync(ItemStockQueryRequest request, CancellationToken ct = default)
    {
        var company = (request.CompanyName ?? "").Trim();
        var item = (request.ItemCode ?? "").Trim();
        if (company.Length == 0)
            throw new ArgumentException("Company is required.");
        if (item.Length == 0)
            throw new ArgumentException("Item code is required.");

        var result = new ItemStockResult
        {
            CompanyName = company,
            ItemCode = item,
            DateTo = request.DateTo.Date,
        };

        if (item.StartsWith("RAW", StringComparison.OrdinalIgnoreCase))
            return result;

        await using var connection = _database.CreateConnection();
        await using var rollCmd = connection.CreateCommand();
        rollCmd.CommandText = "dbo.usp_RollStockAsOn";
        rollCmd.CommandType = CommandType.StoredProcedure;
        rollCmd.CommandTimeout = TimeoutSeconds;
        rollCmd.Parameters.Add("@CompanyName", SqlDbType.VarChar, 150).Value = company;
        rollCmd.Parameters.Add("@AsOnDate", SqlDbType.Date).Value = request.DateTo.Date;
        rollCmd.Parameters.Add("@ItemCode", SqlDbType.VarChar, 50).Value = item;

        await using var reader = await rollCmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            result.Rolls.Add(new ItemRollLine
            {
                Godown = Str(reader, "vToGodown"),
                RollNo = Str(reader, "RollNo"),
                ItemName = Str(reader, "FGItemname"),
                NetWt = Dec(reader, "NetWt"),
                ProducedOn = reader.IsDBNull(reader.GetOrdinal("Sysdate"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("Sysdate")),
            });
        }

        result.RollCount = result.Rolls.Count;
        result.RollNetWt = result.Rolls.Sum(r => r.NetWt);
        if (result.RollCount > 0)
        {
            result.RollNote = "Each roll weight is the weight left on that roll today. A past end date changes which rolls are listed, not the historical weight.";
        }

        return result;
    }

    private static string Str(SqlDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        return reader.IsDBNull(i) ? "" : reader.GetValue(i)?.ToString()?.Trim() ?? "";
    }

    private static decimal Dec(SqlDataReader reader, string name)
    {
        var i = reader.GetOrdinal(name);
        if (reader.IsDBNull(i)) return 0m;
        return Convert.ToDecimal(reader.GetValue(i));
    }
}
