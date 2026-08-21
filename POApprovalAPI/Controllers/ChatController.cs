using Microsoft.AspNetCore.Mvc;
using POApprovalAPI.Models;
using POApprovalAPI.Services;

namespace POApprovalAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatOrchestratorService _chat;

    public ChatController(ChatOrchestratorService chat)
    {
        _chat = chat;
    }

    /// <summary>
    /// Natural-language question → schema RAG → Groq SQL → execute → answer.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] ChatRequest request, CancellationToken ct)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Message is required." });

        try
        {
            var result = await _chat.AskAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Re-run the chat SQL without TOP (up to export cap) and return CSV.
    /// </summary>
    [HttpPost("export")]
    public async Task<IActionResult> Export([FromBody] ChatExportRequest request, CancellationToken ct)
    {
        if (request == null)
            return BadRequest(new { message = "Request body is required." });
        if (string.IsNullOrWhiteSpace(request.Sql) && request.ExportContext == null)
            return BadRequest(new { message = "Sql or exportContext is required." });

        try
        {
            var result = await _chat.ExportAsync(request, ct);
            Response.Headers["X-Row-Count"] = result.RowCount.ToString();
            Response.Headers["X-Truncated"] = result.Truncated ? "true" : "false";
            Response.Headers["X-Total-Count"] = result.TotalCount?.ToString() ?? "";
            return File(result.CsvBytes, "text/csv; charset=utf-8", result.FileName);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
