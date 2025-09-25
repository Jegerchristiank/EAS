using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Models;
using EsgAsAService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("imports/csv")] 
public class ImportController : ControllerBase
{
    private readonly ICsvImportService _import;
    public ImportController(ICsvImportService import) => _import = import;

    [HttpPost("analyze")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<IActionResult> Analyze([FromForm] IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            var problem = new ProblemDetails { Title = "Bad Request", Status = 400, Detail = "file_required" };
            problem.Extensions["code"] = "file_required";
            return BadRequest(problem);
        }
        if (!LooksLikeTextCsv(file))
        {
            var problem = new ProblemDetails { Title = "Unsupported Media Type", Status = 415, Detail = "only_text_csv_supported" };
            problem.Extensions["code"] = "unsupported_media_type";
            return StatusCode(415, problem);
        }
        return Ok(await _import.AnalyzeAsync(file, HttpContext.RequestAborted));
    }

    [HttpPost("commit")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<IActionResult> Commit([FromForm] IFormFile file, [FromForm] Guid organisationId, [FromForm] Guid periodId, [FromForm] string mapping)
    {
        if (file is null || file.Length == 0)
        {
            var problem = new ProblemDetails { Title = "Bad Request", Status = 400, Detail = "file_required" };
            problem.Extensions["code"] = "file_required";
            return BadRequest(problem);
        }
        if (!LooksLikeTextCsv(file))
        {
            var problem = new ProblemDetails { Title = "Unsupported Media Type", Status = 415, Detail = "only_text_csv_supported" };
            problem.Extensions["code"] = "unsupported_media_type";
            return StatusCode(415, problem);
        }
        var map = System.Text.Json.JsonSerializer.Deserialize<CsvCommitRequest>(mapping) ?? new CsvCommitRequest(new());
        var n = await _import.CommitAsync(file, map, organisationId, periodId, HttpContext.RequestAborted);
        return Ok(new { imported = n });
    }

    private static bool LooksLikeTextCsv(IFormFile file)
    {
        // Heuristic: content-type contains csv or text; and first bytes decode as UTF8/ASCII
        var ct = file.ContentType?.ToLowerInvariant() ?? string.Empty;
        var okCt = ct.Contains("csv") || ct.StartsWith("text/") || file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
        if (!okCt) return false;
        try
        {
            using var s = file.OpenReadStream();
            var buf = new byte[Math.Min(1024, (int)Math.Max(1, file.Length))];
            var read = s.Read(buf, 0, buf.Length);
            var text = System.Text.Encoding.UTF8.GetString(buf, 0, read);
            // Accept simple CSV with either separators or newline header; reject if binary (NUL)
            return text.IndexOf('\0') < 0 && (text.IndexOfAny(new[] { ',', ';' }) >= 0 || text.Contains('\n'));
        }
        catch
        {
            return false;
        }
    }
}
