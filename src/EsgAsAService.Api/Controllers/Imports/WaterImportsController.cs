using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using EsgAsAService.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace EsgAsAService.Api.Controllers.Importing;

/// <summary>
/// Water import flow stores the raw upload in staging and processes rows into WaterMeter records.
/// Why: keep ingestion idempotent and flexible on units so we can safely re-run ETL and accept vendor CSVs.
/// </summary>
[ApiController]
[EnableRateLimiting("ingest")]
[Route("imports/water")] 
public class WaterImportsController(EsgDbContext db, IUnitConversionService units) : ControllerBase
{
    private readonly EsgDbContext _db = db;
    private readonly IUnitConversionService _units = units;

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
        var folder = Path.Combine(AppContext.BaseDirectory, "staging", "water");
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
            Type = "water_invoice",
            Source = "upload",
            PayloadJson = JsonSerializer.Serialize(meta)
        };
        _db.Add(doc);
        await _db.SaveChangesAsync();
        return CreatedAtRoute("WaterProcess", new { docId = doc.Id }, new { staged = doc.Id });
    }

    [HttpPost("process/{docId:guid}", Name = "WaterProcess")]
    [Authorize(Policy = "CanIngestData")]
    public async Task<IActionResult> Process(Guid docId)
    {
        var doc = await _db.StagingDocuments.FindAsync(docId);
        if (doc is null)
        { var p = new ProblemDetails { Title = "Not Found", Status = 404, Detail = "doc_not_found" }; p.Extensions["code"] = "doc_not_found"; return NotFound(p); }
        if (doc.ReportingPeriodId is null)
        { var p = new ProblemDetails { Title = "Bad Request", Status = 400, Detail = "period_required" }; p.Extensions["code"] = "period_required"; return BadRequest(p); }
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
        // Accept either normalized columns in m3 or a pair of value+unit columns to convert.
        // Why: suppliers differ; we normalize to m3 as canonical unit in storage.
        int idxIntakeM3 = Array.FindIndex(headers, h => string.Equals(h, "intake_m3", StringComparison.OrdinalIgnoreCase));
        int idxDischargeM3 = Array.FindIndex(headers, h => string.Equals(h, "discharge_m3", StringComparison.OrdinalIgnoreCase));
        int idxIntake = Array.FindIndex(headers, h => string.Equals(h, "intake", StringComparison.OrdinalIgnoreCase));
        int idxDischarge = Array.FindIndex(headers, h => string.Equals(h, "discharge", StringComparison.OrdinalIgnoreCase));
        int idxIntakeUnit = Array.FindIndex(headers, h => string.Equals(h, "intake_unit", StringComparison.OrdinalIgnoreCase));
        int idxDischargeUnit = Array.FindIndex(headers, h => string.Equals(h, "discharge_unit", StringComparison.OrdinalIgnoreCase));
        if (idxIntakeM3 < 0 && idxIntake < 0)
        { var pd = new ProblemDetails { Title = "Bad Request", Status = 400, Detail = "required_columns_missing" }; pd.Extensions["code"] = "required_columns_missing"; return BadRequest(pd); }

        while (await csv.ReadAsync())
        {
            var lineNo = csv.Context.Parser.Row;
            double intake;
            double? discharge = null;
            if (idxIntake >= 0)
            {
                var intakeStr = csv.GetField(idxIntake);
                if (!double.TryParse(intakeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var intakeRaw))
                    continue;
                var intakeUnitCode = (idxIntakeUnit >= 0 ? csv.GetField(idxIntakeUnit) : "m3")?.Trim();
                var from = await _db.Units.FirstOrDefaultAsync(u => u.Code == (intakeUnitCode ?? "m3"));
                var to = await _db.Units.FirstOrDefaultAsync(u => u.Code == "m3");
                if (from is null || to is null) continue;
                try { intake = await _units.ConvertAsync(from.Id, to.Id, intakeRaw); }
                catch { continue; }
            }
            else
            {
                var intakeStr = csv.GetField(idxIntakeM3);
                if (!double.TryParse(intakeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                    continue;
                intake = v;
            }

            if (idxDischarge >= 0)
            {
                var dischargeStr = csv.GetField(idxDischarge);
                if (!string.IsNullOrWhiteSpace(dischargeStr) && double.TryParse(dischargeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var dRaw))
                {
                    var dischargeUnitCode = (idxDischargeUnit >= 0 ? csv.GetField(idxDischargeUnit) : "m3")?.Trim();
                    var fromD = await _db.Units.FirstOrDefaultAsync(u => u.Code == (dischargeUnitCode ?? "m3"));
                    var toD = await _db.Units.FirstOrDefaultAsync(u => u.Code == "m3");
                    if (fromD is not null && toD is not null)
                    {
                        try { discharge = await _units.ConvertAsync(fromD.Id, toD.Id, dRaw); }
                        catch { discharge = null; }
                    }
                }
            }
            else if (idxDischargeM3 >= 0)
            {
                var dischargeStr = csv.GetField(idxDischargeM3);
                if (!string.IsNullOrWhiteSpace(dischargeStr) && double.TryParse(dischargeStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    discharge = d;
            }
            // Idempotency strategy: persist a normalized JSON payload per processed row
            // and skip future rows with identical normalized payload for the same document.
            // Why: vendors sometimes resend identical files or add padding rows; this avoids duplicates
            // without relying on line numbers only (which may change between exports).
            var norm = JsonSerializer.Serialize(new { intake_m3 = intake, discharge_m3 = discharge });
            // Cross-document de-dup within same org/period/type. Why: vendors can split identical
            // content across multiple uploads; we dedupe at normalized payload level.
            if (await _db.StagingLines.AnyAsync(x => x.DocumentId == doc.Id && x.Json == norm)
                || await AlreadyProcessedAsync(doc.OrganisationId, doc.ReportingPeriodId, doc.Type, norm))
                continue;
            _db.Add(new WaterMeter { OrganisationId = doc.OrganisationId, ReportingPeriodId = doc.ReportingPeriodId!.Value, IntakeM3 = intake, DischargeM3 = discharge });
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
