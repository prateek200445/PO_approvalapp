using Dapper;
using POApprovalAPI.Models;

namespace POApprovalAPI.Services;

public class PoApprovalService
{
    public const int MaxBulkSize = int.MaxValue;
    private const int BulkParallelism = 8;

    private readonly DatabaseService _database;
    private readonly EmailService _emailService;

    public PoApprovalService(DatabaseService database, EmailService emailService)
    {
        _database = database;
        _emailService = emailService;
    }

    /// <summary>
    /// Approves a single pending PO row. Same rules as the original single-approve endpoint.
    /// When <paramref name="requiredUserName"/> is set, the row must belong to that user and be Pending.
    /// </summary>
    public async Task<PoApproveItemResult> ApproveOneAsync(
        int transId,
        string? remarks,
        string? requiredUserName = null)
    {
        remarks ??= "";

        try
        {
            using var connection = _database.CreateConnection();

            string table = "ApprovePO";
            var po = await connection.QueryFirstOrDefaultAsync(
                @"SELECT PoNo, ApprovalName, Status
                  FROM ApprovePO
                  WHERE TransId = @transId",
                new { transId });

            if (po == null)
            {
                po = await connection.QueryFirstOrDefaultAsync(
                    @"SELECT PoNo, ApprovalName, Status
                      FROM ApprovePOHOD
                      WHERE TransId = @transId",
                    new { transId });

                if (po != null)
                    table = "ApprovePOHOD";
            }

            if (po == null)
            {
                return Fail(transId, null, "PO approval row not found");
            }

            string poNo = Convert.ToString(po.PoNo) ?? "";
            string approvalName = Convert.ToString(po.ApprovalName) ?? "";
            string status = Convert.ToString(po.Status) ?? "";

            if (!string.IsNullOrWhiteSpace(requiredUserName) &&
                !string.Equals(approvalName, requiredUserName, StringComparison.OrdinalIgnoreCase))
            {
                return Fail(transId, poNo, "Not assigned to this user");
            }

            if (!string.Equals(status, "Pending", StringComparison.OrdinalIgnoreCase))
            {
                return Fail(transId, poNo, $"Not pending (current status: {status})");
            }

            if (table == "ApprovePOHOD")
                return await ApproveHodAsync(connection, transId, poNo);

            // Authority is allocated per company in poallocation ('*' = all companies).
            var approvalData = await connection.QueryFirstOrDefaultAsync<ApprovalData>(
                @"SELECT
                    CAST(@PoNo AS varchar(50)) AS PoNo,
                    CAST(@ApprovalName AS varchar(100)) AS ApprovalName,
                    lr.email AS Email,
                    ISNULL(pa.authority, 0) AS Authority
                  FROM (SELECT 1 AS n) dummy
                  LEFT JOIN PurchasePayment pp ON pp.PurchaseCode = @PoNo
                  OUTER APPLY (
                      SELECT TOP 1 x.authority
                      FROM poallocation x
                      WHERE x.username = @ApprovalName
                      ORDER BY CASE WHEN x.CompanyName = pp.CompanyName THEN 0
                                    WHEN x.CompanyName = '*' THEN 1
                                    ELSE 2 END,
                               x.authority
                  ) pa
                  LEFT JOIN loginentry..loginrights lr ON lr.NAME = pp.LOGINNAME",
                new { PoNo = poNo, ApprovalName = approvalName });

            if (approvalData == null)
            {
                approvalData = new ApprovalData
                {
                    PoNo = poNo,
                    ApprovalName = approvalName,
                    Email = "",
                    Authority = 0
                };
            }

            if (approvalData.Authority == 1)
            {
                // Approve this user's row only if still Pending
                var selfRows = await connection.ExecuteAsync(
                    $@"UPDATE {table}
                       SET Status = 'Approved',
                           ApprovalDate = GETDATE()
                       WHERE PoNo = @PoNo
                         AND ApprovalName = @ApprovalName
                         AND Status = 'Pending'",
                    new
                    {
                        PoNo = approvalData.PoNo,
                        ApprovalName = approvalData.ApprovalName
                    });

                if (selfRows <= 0)
                    return Fail(transId, poNo, "Update did not affect any rows (may already be processed)");

                // Close other pending approvers; stamp same ApprovalDate as authority-1 approver
                await connection.ExecuteAsync(
                    $@"UPDATE t
                       SET Status = @OtherStatus,
                           ApprovalDate = a.ApprovalDate
                       FROM {table} t
                       INNER JOIN {table} a
                         ON a.PoNo = t.PoNo
                        AND a.ApprovalName = @ApprovalName
                       WHERE t.PoNo = @PoNo
                         AND t.ApprovalName <> @ApprovalName
                         AND t.Status = 'Pending'",
                    new
                    {
                        PoNo = approvalData.PoNo,
                        ApprovalName = approvalData.ApprovalName,
                        OtherStatus = $"Approved by {approvalData.ApprovalName}"
                    });

                await connection.ExecuteAsync(
                    @"UPDATE PurchasePayment
                      SET PoSignal = '*'
                      WHERE PurchaseCode = @PoNo",
                    new { PoNo = approvalData.PoNo });

                if (!string.IsNullOrWhiteSpace(approvalData.Email))
                {
                    await _emailService.SendMail(
                        approvalData.Email,
                        $"PO {approvalData.PoNo} Approved",
                        $"Dear Sir,\n\n" +
                        $"PO Number: {approvalData.PoNo}\n" +
                        $"Approved By: {approvalData.ApprovalName}\n" +
                        $"Remarks: {remarks}\n\n" +
                        $"Regards,\n" +
                        $"{approvalData.ApprovalName}"
                    );
                }

                return Ok(transId, approvalData.PoNo);
            }

            var intermediateRows = await connection.ExecuteAsync(
                $@"UPDATE {table}
                   SET Status = 'Approved',
                       ApprovalDate = GETDATE()
                   WHERE TransId = @transId
                     AND Status = 'Pending'",
                new { transId });

            if (intermediateRows <= 0)
                return Fail(transId, poNo, "Update did not affect any rows (may already be processed)");

            // ERP rule: '*' once an authority-1 user has approved (it may already be set from the ERP) or nothing is
            // left pending; otherwise '#'. Never downgrade an approved '*' back to '#'.
            await connection.ExecuteAsync(
                @"UPDATE PurchasePayment
                  SET PoSignal = CASE
                      WHEN PoSignal = '*' THEN '*'
                      WHEN NOT EXISTS (
                          SELECT 1 FROM ApprovePO
                          WHERE PoNo = @PoNo
                            AND (Status = 'Pending' OR Status LIKE 'Reject%')) THEN '*'
                      ELSE '#' END
                  WHERE PurchaseCode = @PoNo",
                new { PoNo = approvalData.PoNo });

            return Ok(transId, approvalData.PoNo);
        }
        catch (Exception ex)
        {
            return Fail(transId, null, ex.Message);
        }
    }

    /// <summary>
    /// HOD stage: approve the HOD row; once no HOD approval is pending, create the ApprovePO rows for the next level
    /// exactly as the ERP does (sp_GetPOAllocationUsers by PO company and department). The PO stays '#' until the
    /// ApprovePO stage completes.
    /// </summary>
    private static async Task<PoApproveItemResult> ApproveHodAsync(System.Data.IDbConnection connection, int transId, string poNo)
    {
        using var tx = connection.BeginTransaction();

        var rows = await connection.ExecuteAsync(
            @"UPDATE ApprovePOHOD
              SET Status = 'Approved',
                  ApprovalDate = GETDATE()
              WHERE TransId = @transId
                AND Status = 'Pending'",
            new { transId }, tx);

        if (rows <= 0)
        {
            tx.Rollback();
            return Fail(transId, poNo, "Update did not affect any rows (may already be processed)");
        }

        await connection.ExecuteAsync(
            @"IF NOT EXISTS (
                  SELECT 1 FROM ApprovePOHOD WITH (UPDLOCK, HOLDLOCK)
                  WHERE PoNo = @PoNo AND (Status = 'Pending' OR Status LIKE 'Reject%'))
              AND NOT EXISTS (SELECT 1 FROM ApprovePO WITH (UPDLOCK, HOLDLOCK) WHERE PoNo = @PoNo)
              BEGIN
                  DECLARE @Company varchar(100) = (SELECT TOP 1 CompanyName FROM PurchasePayment WHERE PurchaseCode = @PoNo);
                  DECLARE @Deptt varchar(50) = (SELECT TOP 1 ISNULL(depttname, deptt) FROM vw_Purchaseorder WHERE purchasecode = @PoNo);
                  DECLARE @PODate datetime = (SELECT TOP 1 PODate FROM ApprovePOHOD WHERE PoNo = @PoNo ORDER BY transid);
                  DECLARE @Users TABLE (ApprovalName varchar(255), Authority int);
                  INSERT INTO @Users EXEC sp_GetPOAllocationUsers @Company, @Deptt;
                  INSERT INTO ApprovePO (PoNo, ApprovalName, Status, PODate, ApprovalDate)
                  SELECT @PoNo, ApprovalName, 'Pending', @PODate, NULL FROM @Users;
              END

              UPDATE PurchasePayment
              SET PoSignal = '#'
              WHERE PurchaseCode = @PoNo AND ISNULL(PoSignal, '') <> '*';",
            new { PoNo = poNo }, tx);

        tx.Commit();
        return Ok(transId, poNo);
    }

    public async Task<PoBulkApproveResponse> ApproveBulkAsync(PoBulkApproveRequest request)
    {
        var response = new PoBulkApproveResponse();

        if (request == null || request.TransIds == null || request.TransIds.Count == 0)
        {
            return response;
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            response.Failed.Add(new PoApproveItemResult
            {
                TransId = 0,
                Success = false,
                Reason = "UserName is required for bulk approve"
            });
            response.Total = request.TransIds.Count;
            return response;
        }

        var distinctIds = request.TransIds.Where(id => id > 0).Distinct().ToList();
        response.Total = distinctIds.Count;

        if (distinctIds.Count > MaxBulkSize)
        {
            response.Failed.Add(new PoApproveItemResult
            {
                TransId = 0,
                Success = false,
                Reason = $"Too many items. Maximum is {MaxBulkSize} per request."
            });
            return response;
        }

        var succeeded = new System.Collections.Concurrent.ConcurrentBag<PoApproveItemResult>();
        var failed = new System.Collections.Concurrent.ConcurrentBag<PoApproveItemResult>();
        var remarks = request.Remarks ?? "";
        var userName = request.UserName.Trim();

        await Parallel.ForEachAsync(
            distinctIds,
            new ParallelOptions { MaxDegreeOfParallelism = BulkParallelism },
            async (transId, _) =>
            {
                var result = await ApproveOneAsync(transId, remarks, userName);
                if (result.Success)
                    succeeded.Add(result);
                else
                    failed.Add(result);
            });

        response.Succeeded = succeeded.OrderBy(x => x.TransId).ToList();
        response.Failed = failed.OrderBy(x => x.TransId).ToList();
        return response;
    }

    private static PoApproveItemResult Ok(int transId, string? poNo) => new()
    {
        TransId = transId,
        PoNo = poNo,
        Success = true
    };

    private static PoApproveItemResult Fail(int transId, string? poNo, string reason) => new()
    {
        TransId = transId,
        PoNo = poNo,
        Success = false,
        Reason = reason
    };
}
