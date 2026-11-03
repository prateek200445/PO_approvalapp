using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/gate")]
public class GateController : ControllerBase
{
    private readonly GateService _service;
    private readonly ILogger<GateController> _logger;

    public GateController(GateService service, ILogger<GateController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("context")]
    public Task<IActionResult> Context([FromQuery] string username, CancellationToken ct) =>
        Run(() => _service.GetContextAsync(username, ct));

    [HttpGet("document")]
    public Task<IActionResult> Document(
        [FromQuery] string username,
        [FromQuery] string number,
        [FromQuery] string? documentType,
        CancellationToken ct) =>
        Run(() => _service.LookupDocumentAsync(username, number, documentType, ct));

    [HttpGet("inside")]
    public Task<IActionResult> Inside(
        [FromQuery] string username,
        [FromQuery] string? factoryCode,
        [FromQuery] string? q,
        CancellationToken ct) =>
        Run(() => _service.ListInsideAsync(username, factoryCode, q, ct));

    [HttpGet("summary")]
    public Task<IActionResult> Summary([FromQuery] string username, [FromQuery] string? factoryCode, CancellationToken ct) =>
        Run(() => _service.GetSummaryAsync(username, factoryCode, ct));

    [HttpGet("register")]
    public Task<IActionResult> Register(
        [FromQuery] string username,
        [FromQuery] string? factoryCode,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? vehicleNumber,
        [FromQuery] string? driver,
        [FromQuery] string? party,
        [FromQuery] string? purposeCode,
        [FromQuery] string? status,
        [FromQuery] bool longStayOnly,
        CancellationToken ct) =>
        Run(() => _service.RegisterAsync(new GateRegisterQuery
        {
            Username = username,
            FactoryCode = factoryCode,
            DateFrom = dateFrom,
            DateTo = dateTo,
            VehicleNumber = vehicleNumber,
            Driver = driver,
            Party = party,
            PurposeCode = purposeCode,
            Status = status,
            LongStayOnly = longStayOnly,
        }, ct));

    [HttpGet("register/excel")]
    public async Task<IActionResult> Excel(
        [FromQuery] string username,
        [FromQuery] string? factoryCode,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? vehicleNumber,
        [FromQuery] string? driver,
        [FromQuery] string? party,
        [FromQuery] string? purposeCode,
        [FromQuery] string? status,
        [FromQuery] bool longStayOnly,
        CancellationToken ct)
    {
        try
        {
            var (bytes, fileName) = await _service.ExportExcelAsync(new GateRegisterQuery
            {
                Username = username,
                FactoryCode = factoryCode,
                DateFrom = dateFrom,
                DateTo = dateTo,
                VehicleNumber = vehicleNumber,
                Driver = driver,
                Party = party,
                PurposeCode = purposeCode,
                Status = status,
                LongStayOnly = longStayOnly,
            }, ct);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            return Map(ex);
        }
    }

    [HttpGet("entries/{entryId:long}/audit")]
    public Task<IActionResult> Audit([FromQuery] string username, long entryId, CancellationToken ct) =>
        Run(() => _service.AuditAsync(username, entryId, ct));

    [HttpPost("entries")]
    public async Task<IActionResult> Create([FromBody] GateEntryCreateRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _service.CreateEntryAsync(request, ct);
            return StatusCode(StatusCodes.Status201Created, created);
        }
        catch (Exception ex)
        {
            return Map(ex);
        }
    }

    [HttpPost("entries/{entryId:long}/exit")]
    public Task<IActionResult> Exit(long entryId, [FromBody] GateExitRequest request, CancellationToken ct) =>
        Run(() => _service.ExitAsync(entryId, request, ct));

    [HttpPost("entries/{entryId:long}/correct")]
    public Task<IActionResult> Correct(long entryId, [FromBody] GateCorrectRequest request, CancellationToken ct) =>
        Run(() => _service.CorrectAsync(entryId, request, ct));

    [HttpPost("gates")]
    public async Task<IActionResult> AddGate([FromBody] GateCreateRequest request, CancellationToken ct)
    {
        try
        {
            var created = await _service.AddGateAsync(request, ct);
            return StatusCode(StatusCodes.Status201Created, created);
        }
        catch (Exception ex)
        {
            return Map(ex);
        }
    }

    private async Task<IActionResult> Run<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (Exception ex)
        {
            return Map(ex);
        }
    }

    private IActionResult Map(Exception ex)
    {
        if (ex is not ArgumentException and not UnauthorizedAccessException and not InvalidOperationException and not KeyNotFoundException)
            _logger.LogError(ex, "Gate request failed");
        return ex switch
        {
            ArgumentException => BadRequest(new { message = ex.Message }),
            UnauthorizedAccessException => StatusCode(403, new { message = ex.Message }),
            InvalidOperationException => Conflict(new { message = ex.Message }),
            KeyNotFoundException => NotFound(new { message = ex.Message }),
            SqlException => StatusCode(500, new { message = "The gate register could not be reached. Please try again." }),
            _ => StatusCode(500, new { message = "The gate register could not be reached. Please try again." }),
        };
    }
}