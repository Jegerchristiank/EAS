using System.Security.Cryptography;
using System.Text;
using EsgAsAService.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace EsgAsAService.Infrastructure.Diagnostics;

using System.Runtime.CompilerServices;

public class DiagnosticService(ILogger<DiagnosticService> logger) : IDiagnosticService
{
    private readonly ILogger<DiagnosticService> _logger = logger;

    public string Capture(Exception ex, string area, object? data = null,
        string? code = null,
        [CallerFilePath] string? file = null,
        [CallerMemberName] string? member = null,
        [CallerLineNumber] int line = 0)
    {
        var now = DateTimeOffset.UtcNow;
        // Stable code derived from logical area (e.g., Wizard.SaveAll -> WIZ-SAVEALL)
        var staticCode = code ?? MakeStaticCode(area);
        var ticket = MakeTicket(now);
        // Returned code format keeps backward compatibility and clarity:
        // ESG-<STATIC>-<TICKET>
        var returned = $"ESG-{staticCode}-{ticket.Suffix}";

        _logger.LogError(ex,
            "Diagnostic {Returned} in {Area} at {Utc}: {@Data} [ticket {TicketFull} at {File}:{Line} {Member}]",
            returned, area, now, data, ticket.Full, file, line, member);

        return returned;
    }

    private static string MakeStaticCode(string area)
    {
        // Compact common prefixes to keep codes short and recognizable
        string prefix = area;
        if (area.StartsWith("Wizard.", StringComparison.OrdinalIgnoreCase))
            prefix = "WIZ-" + area[7..];
        else if (area.StartsWith("API", StringComparison.OrdinalIgnoreCase))
            prefix = area; // keep as-is (e.g., API.Controllers.Action)

        var slug = new StringBuilder(prefix.Length + 8);
        foreach (var ch in prefix)
        {
            if (char.IsLetterOrDigit(ch)) slug.Append(char.ToUpperInvariant(ch));
            else if (ch == '.' || ch == '/' || ch == '_' || ch == '-') slug.Append('-');
        }
        return slug.ToString().Trim('-');
    }

    private static (string Full, string Suffix) MakeTicket(DateTimeOffset now)
    {
        var stamp = now.ToString("yyyyMMddHHmmss");
        var nonce = RandomNumberGenerator.GetBytes(6); // 48 bits
        var suffix = ToBase32(nonce);
        return ($"ESG-{stamp}-{suffix}", suffix); // full for logs, suffix for returned code
    }

    private static string ToBase32(ReadOnlySpan<byte> bytes)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder();
        int bitBuffer = 0;
        int bitCount = 0;
        foreach (var b in bytes)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitCount += 8;
            while (bitCount >= 5)
            {
                int index = (bitBuffer >> (bitCount - 5)) & 31;
                output.Append(alphabet[index]);
                bitCount -= 5;
            }
        }
        if (bitCount > 0)
        {
            int index = (bitBuffer << (5 - bitCount)) & 31;
            output.Append(alphabet[index]);
        }
        return output.ToString();
    }
}
