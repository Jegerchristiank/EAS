using System.Text.Json;
using EsgAsAService.Domain.Entities.Core;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using EsgAsAService.Infrastructure.Data;
using EsgAsAService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace EsgAsAService.Api.Controllers.Importing;

/// <summary>
/// Waste manifest import. Why: normalize quantity to kg and ensure idempotent processing by
/// tracking a normalized payload-per-line per staging document.
/// </summary>
[ApiController]
[EnableRateLimiting("ingest")]
[Route("imports/waste")] 
public class WasteImportsController(EsgDbContext db, IUnitConversionService units) : ControllerBase
{
    private readonly EsgDbContext _db = db;
    private readonly IUnitConversionService _units = units;

    [HttpPost("manifest")]
    [Authorize(Policy = "CanIngestData")]
    [RequestSizeLimit(25_000_000)]
    public async Task<IActionResult> Manifest([FromForm] Guid organisationId, [FromForm] Guid? periodId, [FromForm] IFormFile file)
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
        var folder = Path.Combine(AppContext.BaseDirectory, "staging", "waste");
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
            Type = "waste_manifest",
            Source = "upload",
            PayloadJson = JsonSerializer.Serialize(meta)
        };
        _db.Add(doc);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Process), new { docId = doc.Id }, new { staged = doc.Id });
    }

    [HttpPost("process/{docId:guid}")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<IActionResult> Process(Guid docId)
    {
        var doc = await _db.StagingDocuments.FindAsync(docId);
        if (doc is null) return NotFound(new { error = "doc_not_found" });
        if (doc.ReportingPeriodId is null) return BadRequest(new { error = "period_required" });
        var meta = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(doc.PayloadJson ?? "{}");
        if (!meta.TryGetProperty("path", out var p)) return BadRequest(new { error = "missing_path" });
        var path = p.GetString();
        if (string.IsNullOrWhiteSpace(path) || !System.IO.File.Exists(path)) return BadRequest(new { error = "file_missing" });

        int imported = 0;
        using var reader = new StreamReader(path!);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { DetectDelimiter = true });
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = csv.HeaderRecord ?? Array.Empty<string>();
        // Support both already-normalized quantity_kg and raw quantity+unit for conversion to kg.
        int idxEak = Array.FindIndex(headers, h => string.Equals(h, "eak_code", StringComparison.OrdinalIgnoreCase));
        int idxQtyKg = Array.FindIndex(headers, h => string.Equals(h, "quantity_kg", StringComparison.OrdinalIgnoreCase));
        int idxQty = Array.FindIndex(headers, h => string.Equals(h, "quantity", StringComparison.OrdinalIgnoreCase));
        int idxUnit = Array.FindIndex(headers, h => string.Equals(h, "unit", StringComparison.OrdinalIgnoreCase));
        int idxDisp = Array.FindIndex(headers, h => string.Equals(h, "disposition", StringComparison.OrdinalIgnoreCase));
        if (idxEak < 0 || (idxQtyKg < 0 && idxQty < 0) || idxDisp < 0) return BadRequest(new { error = "required_columns_missing" });

        while (await csv.ReadAsync())
        {
            var lineNo = csv.Context.Parser.Row;

            var eak = csv.GetField(idxEak)?.Trim();
            double qtyKg;
            if (idxQty >= 0)
            {
                var qtyStr = csv.GetField(idxQty);
                if (!double.TryParse(qtyStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var qtyRaw)) continue;
                var code = (idxUnit >= 0 ? csv.GetField(idxUnit) : "kg")?.Trim();
                var from = await _db.Units.FirstOrDefaultAsync(u => u.Code == (code ?? "kg"));
                var to = await _db.Units.FirstOrDefaultAsync(u => u.Code == "kg");
                if (from is null || to is null) continue;
                try { qtyKg = await _units.ConvertAsync(from.Id, to.Id, qtyRaw); } catch { continue; }
            }
            else
            {
                var qtyStr = csv.GetField(idxQtyKg);
                if (!double.TryParse(qtyStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var q)) continue;
                qtyKg = q;
            }
            var disp = csv.GetField(idxDisp)?.Trim();
            if (string.IsNullOrWhiteSpace(eak) || string.IsNullOrWhiteSpace(disp)) continue;

            // Why idempotency on normalized payload: manifest lines can be re-sent across batches.
            var norm = System.Text.Json.JsonSerializer.Serialize(new { eak_code = eak, quantity_kg = qtyKg, disposition = disp });
            if (await _db.StagingLines.AnyAsync(x => x.DocumentId == doc.Id && x.Json == norm)
                || await AlreadyProcessedAsync(doc.OrganisationId, doc.ReportingPeriodId, doc.Type, norm))
                continue;

            _db.Add(new WasteManifest
            {
                OrganisationId = doc.OrganisationId,
                ReportingPeriodId = doc.ReportingPeriodId!.Value,
                EakCode = eak!,
                QuantityKg = qtyKg,
                Disposition = disp!
            });
            _db.StagingLines.Add(new StagingLine { DocumentId = doc.Id, LineNo = lineNo, Json = norm });
            imported++;
        }

        doc.ProcessedAt = DateTimeOffset.UtcNow; doc.Status = "processed";
        await _db.SaveChangesAsync();
        return Ok(new { processed = imported });
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
