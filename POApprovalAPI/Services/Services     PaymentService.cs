using Dapper;
using POApprovalAPI.Models;
using System.Data;
namespace POApprovalAPI.Services;

public class PaymentService
{
    private readonly DatabaseService _database;
private readonly EmailService _emailService;

public PaymentService(
    DatabaseService database,
    EmailService emailService)
{
    _database = database;
    _emailService = emailService;
}

    public async Task<IEnumerable<PaymentRequestModel>> GetPendingPayments(
    string username,
    decimal? amount = null,
    string? filterType = null)
    {
        using var connection = _database.CreateConnection();

        var sql = @"
SELECT TOP (20)
    a.PaymentNo,
    e.CompanyName,
    a.PartyName AS VendorName,
    a.BillNo,
    a.MRNo AS MRNNo,
    e.BillDate,
    e.MRNDate,
    e.PaymentDate,
    e.BillAmount,
    a.PaymentAmount,
    e.PaymentTerms,
    e.PriorityLevel,
    e.Currency,
    e.CurrencyRate,
    e.TDS,
    e.DebitNoteAmnt,
    e.Remarks,
    a.LoginName AS RequestedBy,
    CAST(0 AS DECIMAL(18,2)) AS Outstanding,
    CAST(0 AS DECIMAL(18,2)) AS LedgerOSTAmt
FROM BillPaymentHODApproval a
INNER JOIN BillPaymentEntry e
    ON a.PaymentNo = e.PaymentNo
WHERE
    a.Status = 'Pending'
    AND a.ApprovalName = @UserName";
    if (amount != null)
{
    sql += filterType switch
    {
        "lt" => " AND a.PaymentAmount < @Amount",
        "gt" => " AND a.PaymentAmount > @Amount",
        "eq" => " AND a.PaymentAmount = @Amount",
        "lte" => " AND a.PaymentAmount <= @Amount",
        "gte" => " AND a.PaymentAmount >= @Amount",
        _ => ""
    };
}
sql += " ORDER BY e.SysDate DESC";
Console.WriteLine($"Amount = {amount}");
Console.WriteLine($"FilterType = {filterType}");
Console.WriteLine(sql);
       return await connection.QueryAsync<PaymentRequestModel>(
    sql,
    new
    {
        UserName = username,
        Amount = amount
    });
    }

   public async Task<PaymentDetailsModel?> GetPaymentDetails(string paymentNo)
{
    using var connection = _database.CreateConnection();

    var sql = @"SELECT
        PaymentNo,
        CompanyName,
        VendorName,
        BillNo,
        BillDate,
        MRNNo,
        MRNDate,
        BillAmount,
        PaymentTerms,
        PaymentAmount,
        PaymentDate,
        LoginName AS RequestedBy,
        Remarks,
        PriorityLevel,
        LC,
        UTRNo,
        Currency,
        CurrencyRate,
        TDS,
        DebitNoteAmnt,
        PaymentBankName,
        PaymentBankAccNo,
        SpeReq
    FROM BillPaymentEntry
    WHERE PaymentNo = @PaymentNo";
Console.WriteLine($"PaymentNo Received: [{paymentNo}]");

var data = await connection.QueryFirstOrDefaultAsync<PaymentDetailsModel>(
    sql,
    new
    {
        PaymentNo = paymentNo
    });
    if (data != null)
{
    data.LedgerBalance = await connection.ExecuteScalarAsync<decimal?>(
        @"
        SELECT ISNULL(SUM(amount),0)
        FROM vw_LedgerSummary
        WHERE LedgerName = @VendorName",
        new
        {
            data.VendorName
        }) ?? 0;

    data.GroupBalance = await connection.ExecuteScalarAsync<decimal?>(
        @"
        SELECT ISNULL(SUM(amount),0)
        FROM vw_LedgerSummary
        WHERE LedgerName = @VendorName
          AND CompanyName = @CompanyName",
        new
        {
            data.VendorName,
            data.CompanyName
        }) ?? 0;
}

Console.WriteLine(data == null ? "NULL" : "FOUND");

        return data;
    }
    public async Task<IEnumerable<PaymentHistoryModel>> GetPaymentHistory(string paymentNo)
{
    using var connection = _database.CreateConnection();

  var sql = @"
SELECT
    ApprovalName,
    Status,
    ApprovalDate,
    Comment,
    LoginName
FROM BillPaymentEntryApproval
WHERE PaymentNo = @PaymentNo
ORDER BY ApprovalDate";

    return await connection.QueryAsync<PaymentHistoryModel>(
        sql,
        new
        {
            PaymentNo = paymentNo
        });
}
public async Task<bool> ApprovePayment(PaymentApprovalRequest request)
{
    using var connection = _database.CreateConnection();
    using var transaction = connection.BeginTransaction();

    try
    {
        var payment = await connection.QueryFirstOrDefaultAsync(
            @"SELECT PaymentNo,
                     ApprovalName
              FROM BillPaymentHODApproval
              WHERE PaymentNo = @PaymentNo
                AND ApprovalName = @UserName
                AND Status = 'Pending'",
            new
            {
                request.PaymentNo,
                UserName = request.UserName
            },
            transaction);

        if (payment == null)
        {
            transaction.Rollback();
            return false;
        }
           var email = await connection.QueryFirstOrDefaultAsync<string>(
    @"SELECT lr.email
      FROM BillPaymentEntry b
      INNER JOIN loginentry..loginrights lr
          ON lr.NAME = b.LoginName
      WHERE b.PaymentNo = @PaymentNo",
    new
    {
        request.PaymentNo
    },
    transaction); 
        var authority = await connection.QueryFirstOrDefaultAsync<int>(
    @"SELECT Authority
      FROM BillPaymentApprovalAllocation
      WHERE ApprovalName = @UserName",
    new
    {
        UserName = request.UserName
    },
    transaction);
       

       if (authority == 1)
{
    // Final approver

    await connection.ExecuteAsync(
        @"UPDATE BillPaymentHODApproval
          SET Status = 'Approved',
              ApprovalDate = GETDATE()
          WHERE PaymentNo = @PaymentNo
            AND ApprovalName = @UserName",
        new
        {
            request.PaymentNo,
            UserName = request.UserName
        },
        transaction);
        if (!string.IsNullOrWhiteSpace(email))
{
    await _emailService.SendMail(
        email,
        $"Payment {request.PaymentNo} Approved",
        $"Dear Sir,\n\n" +
        $"Payment No: {request.PaymentNo}\n" +
        $"Approved By: {request.UserName}\n" +
        $"Remarks: {request.Comment}\n\n" +
        $"Regards,\n" +
        $"{request.UserName}"
    );
}
    await connection.ExecuteAsync(
        @"UPDATE BillPaymentHODApproval
          SET Status = @Status
          WHERE PaymentNo = @PaymentNo
            AND ApprovalName <> @UserName
            AND Status = 'Pending'",
        new
        {
            request.PaymentNo,
            UserName = request.UserName,
            Status = $"Approved by {request.UserName}"
        },
        transaction);

    await connection.ExecuteAsync(
        @"UPDATE BillPaymentEntry
          SET Status = 'Approved'
          WHERE PaymentNo = @PaymentNo",
        new
        {
            request.PaymentNo
        },
        transaction);
}
else
{
    // Intermediate approver

    await connection.ExecuteAsync(
        @"UPDATE BillPaymentHODApproval
          SET Status = 'Approved',
              ApprovalDate = GETDATE()
          WHERE PaymentNo = @PaymentNo
            AND ApprovalName = @UserName",
        new
        {
            request.PaymentNo,
            UserName = request.UserName
        },
        transaction);
}

transaction.Commit();
return true;
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}
  public async Task<bool> RejectPayment(PaymentApprovalRequest request)
{
    using var connection = _database.CreateConnection();
    using var transaction = connection.BeginTransaction();

    try
    {
        var payment = await connection.QueryFirstOrDefaultAsync(
            @"SELECT PaymentNo
              FROM BillPaymentHODApproval
              WHERE PaymentNo = @PaymentNo
                AND ApprovalName = @UserName
                AND Status = 'Pending'",
            new
            {
                request.PaymentNo,
                UserName = request.UserName
            },
            transaction);

        if (payment == null)
        {
            transaction.Rollback();
            return false;
        }
        var email = await connection.QueryFirstOrDefaultAsync<string>(
    @"SELECT lr.email
      FROM BillPaymentEntry b
      INNER JOIN loginentry..loginrights lr
          ON lr.NAME = b.LoginName
      WHERE b.PaymentNo = @PaymentNo",
    new
    {
        request.PaymentNo
    },
    transaction);

        await connection.ExecuteAsync(
            @"UPDATE BillPaymentHODApproval
              SET Status='Rejected',
                  ApprovalDate=GETDATE(),
                  Comment=@Comment
              WHERE PaymentNo=@PaymentNo
                AND ApprovalName=@UserName",
            new
            {
                request.PaymentNo,
                request.UserName,
                request.Comment
            },
            transaction);

        await connection.ExecuteAsync(
            @"UPDATE BillPaymentEntry
              SET Status='Rejected'
              WHERE PaymentNo=@PaymentNo",
            new
            {
                request.PaymentNo
            },
            transaction);
          if (!string.IsNullOrWhiteSpace(email))
{
    await _emailService.SendMail(
        email,
        $"Payment {request.PaymentNo} Rejected",
        $"Dear Sir,\n\n" +
        $"Payment No: {request.PaymentNo}\n" +
        $"Rejected By: {request.UserName}\n" +
        $"Remarks: {request.Comment}\n\n" +
        $"Regards,\n" +
        $"{request.UserName}"
    );
}  

        transaction.Commit();
        return true;
    }
    catch
    {
        transaction.Rollback();
        throw;
    }
}  
}

