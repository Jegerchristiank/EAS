using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace EsgAsAService.Api.Controllers.Importing;

/// <summary>
/// Ingests supplier energy CSVs by first staging the raw upload and then processing
/// lines into normalized activities + scope entries. Why: decouple IO/parsing from
/// domain writes and keep the flow idempotent across repeated uploads.
/// </summary>
[ApiController]
[EnableRateLimiting("ingest")]
[Route("imports/energy")] 
public class EnergyImportsController(EsgDbContext db) : ControllerBase
{
    private readonly EsgDbContext _db = db;

    [HttpPost("invoice")]
    [Authorize(Policy = "CanIngestData")]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> Invoice([FromForm] Guid organisationId, [FromForm] Guid? periodId, [FromForm] IFormFile file)
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
        var folder = Path.Combine(AppContext.BaseDirectory, "staging", "energy");
        Directory.CreateDirectory(folder);
        var safe = Path.GetFileName(file.FileName);
        var path = Path.Combine(folder, $"{Guid.NewGuid():N}_" + safe);
        using (var fs = System.IO.File.Create(path))
        {
            await file.CopyToAsync(fs);
        }
        var meta = new { file = file.FileName, size = file.Length, contentType = file.ContentType, path };
        var doc = new StagingDocument
        {
            OrganisationId = organisationId,
            ReportingPeriodId = periodId,
            Type = "energy_invoice",
            Source = "upload",
            PayloadJson = JsonSerializer.Serialize(meta)
        };
        _db.Add(doc);
        await _db.SaveChangesAsync();
        // Return 201 with Location to the processing route for this staging document
        return CreatedAtRoute("EnergyProcess", new { docId = doc.Id }, new { staged = doc.Id });
    }

    [HttpPost("process/{docId:guid}", Name = "EnergyProcess")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<IActionResult> Process(Guid docId)
    {
        var doc = await _db.StagingDocuments.FindAsync(docId);
        if (doc is null)
        {
            var p = new ProblemDetails { Title = "Not Found", Status = 404, Detail = "doc_not_found" }; p.Extensions["code"] = "doc_not_found"; return NotFound(p);
        }
        if (doc.ReportingPeriodId is null)
        {
            var p = new ProblemDetails { Title = "Bad Request", Status = 400, Detail = "period_required" }; p.Extensions["code"] = "period_required"; return BadRequest(p);
        }
        var meta = JsonSerializer.Deserialize<JsonElement>(doc.PayloadJson ?? "{}");
        if (!meta.TryGetProperty("path", out var pmeta))
        { var pd = new ProblemDetails { Title = "Bad Request", Status = 400, Detail = "missing_path" }; pd.Extensions["code"] = "missing_path"; return BadRequest(pd); }
        var path = pmeta.GetString();
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path))
        { var pd = new ProblemDetails { Title = "Bad Request", Status = 400, Detail = "file_missing" }; pd.Extensions["code"] = "file_missing"; return BadRequest(pd); }

        int imported = 0;
        using var reader = new StreamReader(path!);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { DetectDelimiter = true });
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        int idxCarrier = Array.FindIndex(headers, h => string.Equals(h, "carrier", StringComparison.OrdinalIgnoreCase));
        int idxQty = Array.FindIndex(headers, h => string.Equals(h, "quantity", StringComparison.OrdinalIgnoreCase));
        int idxUnit = Array.FindIndex(headers, h => string.Equals(h, "unit", StringComparison.OrdinalIgnoreCase));
        int idxDate = Array.FindIndex(headers, h => string.Equals(h, "date", StringComparison.OrdinalIgnoreCase));
        if (idxCarrier < 0 || idxQty < 0 || idxUnit < 0)
        { var pd = new ProblemDetails { Title = "Bad Request", Status = 400, Detail = "required_columns_missing" }; pd.Extensions["code"] = "required_columns_missing"; return BadRequest(pd); }

        while (await csv.ReadAsync())
        {
            var lineNo = csv.Context.Parser.Row;

            var carrier = csv.GetField(idxCarrier)?.Trim();
            var qtyStr = csv.GetField(idxQty);
            var unitCode = csv.GetField(idxUnit)?.Trim();
            DateOnly? date = null;
            if (idxDate >= 0)
            {
                var s = csv.GetField(idxDate);
                if (DateTime.TryParse(s, out var dt)) date = DateOnly.FromDateTime(dt);
            }
            if (string.IsNullOrWhiteSpace(carrier) || string.IsNullOrWhiteSpace(unitCode))
                continue;
            if (!double.TryParse(qtyStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var qty))
                continue;

            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Code == unitCode);
            if (unit is null) continue;

            var act = new Activity
            {
                OrganisationId = doc.OrganisationId,
                ReportingPeriodId = doc.ReportingPeriodId!.Value,
                Category = carrier,
                ActivityDate = date,
                Quantity = qty,
                UnitId = unit.Id
            };
            _db.Add(act);
            var scope = InferScope(carrier);
            var se = new ScopeEntry { ActivityId = act.Id, Scope = scope };
            _db.Add(se);

            // Idempotency: use a normalized payload including date and unit to avoid double-processing
            // of the same commercial reading across re-exports with different row ordering.
            var norm = JsonSerializer.Serialize(new { carrier, qty, unit = unitCode, date });
            if (await _db.StagingLines.AnyAsync(x => x.DocumentId == doc.Id && x.Json == norm)
                || await AlreadyProcessedAsync(doc.OrganisationId, doc.ReportingPeriodId, doc.Type, norm))
                continue;
            _db.StagingLines.Add(new StagingLine { DocumentId = doc.Id, LineNo = lineNo, Json = norm });
            imported++;
        }

        doc.ProcessedAt = DateTimeOffset.UtcNow;
        doc.Status = "processed";
        await _db.SaveChangesAsync();
        return Ok(new { processed = imported });
    }

    private static int InferScope(string carrier)
    {
        var c = carrier.ToLowerInvariant();
        if (c.Contains("electric")) return 2;
        if (c.Contains("district")) return 2;
        return 1;
    }

    private Task<bool> AlreadyProcessedAsync(Guid organisationId, Guid? periodId, string type, string normalized)
        => _db.StagingLines
            .Join(_db.StagingDocuments, sl => sl.DocumentId, sd => sd.Id, (sl, sd) => new { sl, sd })
            .AnyAsync(x => x.sd.OrganisationId == organisationId
                        && x.sd.ReportingPeriodId == periodId
                        && x.sd.Type == type
                        && x.sl.Json == normalized);

        static bool LooksLikeTextCsv(IFormFile file)
        {
            var ct = file.ContentType?.ToLowerInvariant() ?? string.Empty;
            var okCt = ct.Contains("csv") || ct.StartsWith("text/") || file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);
            if (!okCt) return false;
            try
            {
                using var s = file.OpenReadStream();
                var buf = new byte[Math.Min(1024, (int)Math.Max(1, file.Length))];
                var read = s.Read(buf, 0, buf.Length);
                var text = System.Text.Encoding.UTF8.GetString(buf, 0, read);
                return text.IndexOf('\0') < 0 && (text.IndexOfAny(new[] { ',', ';' }) >= 0 || text.Contains('\n'));
            }
            catch { return false; }
        }
    }
