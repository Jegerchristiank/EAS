using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EsgAsAService.Domain.Entities.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace EsgAsAService.Api.Services;

public record CsvAnalysisResult(IReadOnlyList<string> Headers, int RowCount);
public record CsvCommitRequest(Dictionary<string,string> Mapping); // csvCol->field

/// <summary>
/// Analyzes and imports CSV files to staging and core tables.
/// Why: isolate parsing/mapping and keep controllers thin and testable.
/// </summary>
public interface ICsvImportService
{
    Task<CsvAnalysisResult> AnalyzeAsync(IFormFile file, CancellationToken ct);
    Task<int> CommitAsync(IFormFile file, CsvCommitRequest request, Guid organisationId, Guid periodId, CancellationToken ct);
}

/// <summary>
/// CSV import helper for energy activities. Why: keep parsing/mapping isolated and testable,
/// and use UnitConversionService to normalize quantities up front to reduce downstream complexity.
/// </summary>
public class CsvImportService : ICsvImportService
{
    private readonly EsgAsAService.Infrastructure.Data.EsgDbContext _db;
    private readonly IUnitConversionService _units;

    public CsvImportService(EsgAsAService.Infrastructure.Data.EsgDbContext db, IUnitConversionService units)
    {
        _db = db; _units = units;
    }

    public async Task<CsvAnalysisResult> AnalyzeAsync(IFormFile file, CancellationToken ct)
    {
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { DetectDelimiter = true });
        await csv.ReadAsync();
        csv.ReadHeader();
        var headers = (IReadOnlyList<string>)(csv.HeaderRecord ?? Array.Empty<string>());
        var rows = 0;
        while (await csv.ReadAsync()) rows++;
        return new CsvAnalysisResult(headers, rows);
    }

    public async Task<int> CommitAsync(IFormFile file, CsvCommitRequest request, Guid organisationId, Guid periodId, CancellationToken ct)
    {
        var useTx = _db.Database.IsRelational();
        IAsyncDisposable? tx = null;
        if (useTx)
        {
            tx = await _db.Database.BeginTransactionAsync(ct);
        }
        using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { DetectDelimiter = true });
        await csv.ReadAsync();
        csv.ReadHeader();

        var fieldMap = request.Mapping; // expects keys: date, category, quantity, unitCode, toUnitCode(optional)
        var imported = 0;
        while (await csv.ReadAsync())
        {
            var category = csv.GetField(fieldMap["category"]) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new InvalidOperationException("Missing required category field");
            }
            var qtyStr = csv.GetField(fieldMap["quantity"]);
            var unitCode = csv.GetField(fieldMap["unitCode"]);
            DateOnly? actDate = null;
            if (fieldMap.TryGetValue("date", out var dateCol))
            {
                var s = csv.GetField(dateCol);
                if (DateTime.TryParse(s, out var dt)) actDate = DateOnly.FromDateTime(dt);
            }

            var unit = await _db.Units.FirstOrDefaultAsync(u => u.Code == unitCode, ct)
                       ?? throw new InvalidOperationException($"Unknown unit {unitCode}");

            // If mapping supplies a target unit, require conversion exists
            if (fieldMap.TryGetValue("toUnitCode", out var toUnitCol))
            {
                var toUnitCode = csv.GetField(toUnitCol);
                var toUnit = await _db.Units.FirstOrDefaultAsync(u => u.Code == toUnitCode, ct)
                             ?? throw new InvalidOperationException($"Unknown target unit {toUnitCode}");
                if (toUnit.Id != unit.Id)
                {
                    var hasConv = await _db.UnitConversions.AnyAsync(c => c.FromUnitId == unit.Id && c.ToUnitId == toUnit.Id, ct);
                    if (!hasConv) throw new InvalidOperationException($"Missing unit conversion from {unit.Code} to {toUnit.Code}");
                }
            }

            if (!double.TryParse(qtyStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var qty))
            {
                throw new InvalidOperationException("Invalid quantity");
            }

            var activity = new Activity
            {
                OrganisationId = organisationId,
                ReportingPeriodId = periodId,
                Category = category,
                ActivityDate = actDate,
                Quantity = qty,
                UnitId = unit.Id
            };
            _db.Add(activity);

            // default ScopeEntry as Scope 1 unless mapping supplies it
            int scope = 1;
            if (fieldMap.TryGetValue("scope", out var scopeCol))
            {
                int.TryParse(csv.GetField(scopeCol), out scope);
                scope = Math.Clamp(scope, 1, 3);
            }
            var scopeEntry = new ScopeEntry { ActivityId = activity.Id, Scope = scope };
            _db.Add(scopeEntry);

            imported++;
        }

        await _db.SaveChangesAsync(ct);
        if (useTx && tx is not null)
        {
            await ((Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction)tx).CommitAsync(ct);
        }
        return imported;
    }
}
