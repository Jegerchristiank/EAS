using EsgAsAService.Api.Auth;
using EsgAsAService.Api.Models;
using EsgAsAService.Domain.Entities.Core;
using EsgAsAService.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EsgAsAService.Api.Controllers;

[ApiController]
[Route("evidence")]
public class EvidenceController : ControllerBase
{
    private readonly EsgDbContext _db;
    private static readonly Dictionary<string, (Guid scopeEntryId, string fileName)> _uploads = new();
    public EvidenceController(EsgDbContext db) => _db = db;

    // Issue a pseudo pre-signed URL token for local upload
[HttpPost("presign")]
[Authorize(Policy = "CanIngestData")]
public ActionResult<EvidenceUploadResponse> GetPresigned([FromBody] EvidenceUploadRequest req)
    {
        if (req is null) return BadRequest();
        var token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()).TrimEnd('=');
        _uploads[token] = (req.ScopeEntryId, req.FileName);
        var absoluteBase = new Uri($"{Request.Scheme}://{Request.Host}");
        var url = new Uri(absoluteBase, $"/evidence/upload/{token}");
        return Ok(new EvidenceUploadResponse(url, token));
    }

    // Client uploads the file with PUT to the presigned URL
[HttpPut("upload/{token}")]
[Authorize(Policy = "CanIngestData")]
public async Task<IActionResult> Upload(string token)
{
    if (!_uploads.TryGetValue(token, out var meta)) return NotFound();
    // Basic content-type and size checks
    var ct = Request.ContentType ?? "application/octet-stream";
    var allowed = new[] { "application/pdf", "image/png", "image/jpeg" };
    if (!allowed.Contains(ct))
    {
        return BadRequest(new[] { new { code = "invalid_mime", field = "file", message = $"Content-Type {ct} not allowed" } });
    }
    const long maxBytes = 10 * 1024 * 1024; // 10MB
    if (Request.ContentLength is long len && len > maxBytes)
    {
        return BadRequest(new[] { new { code = "file_too_large", field = "file", message = $"Max {maxBytes} bytes" } });
    }
        using var ms = new MemoryStream();
        await Request.Body.CopyToAsync(ms);
        var folder = Path.Combine(AppContext.BaseDirectory, "blobs");
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, meta.fileName);
    var data = ms.ToArray();
    if (data.LongLength > maxBytes)
    {
        return BadRequest(new[] { new { code = "file_too_large", field = "file", message = $"Max {maxBytes} bytes" } });
    }
    // Sanitize filename to prevent path traversal
    var safeName = Path.GetFileName(meta.fileName);
    if (string.IsNullOrWhiteSpace(safeName)) return BadRequest(new[] { new { code = "invalid_name", field = "file", message = "Invalid file name" } });
    // Use a unique prefix to avoid collisions
    var finalPath = Path.Combine(folder, $"{Guid.NewGuid():N}_" + safeName);
    await System.IO.File.WriteAllBytesAsync(finalPath, data);

        var doc = new EvidenceDocument
        {
            ScopeEntryId = meta.scopeEntryId,
            FileName = meta.fileName,
            BlobUrl = finalPath
        };
        _db.Add(doc);
        await _db.SaveChangesAsync();
        _uploads.Remove(token);
        return Ok(new { ok = true });
    }
}
