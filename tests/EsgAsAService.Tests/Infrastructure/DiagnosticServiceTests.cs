using System;
using EsgAsAService.Application.Abstractions;
using EsgAsAService.Infrastructure.Diagnostics;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EsgAsAService.Tests.Infrastructure;

public class DiagnosticServiceTests
{
    [Fact]
    public void CaptureReturnsCodeAndUsesExpectedFormat()
    {
        IDiagnosticService svc = new DiagnosticService(NullLogger<DiagnosticService>.Instance);
        var ex = new InvalidOperationException("boom");
        var code = svc.Capture(ex, "UnitTest", new { user = "tester" });

        code.Should().StartWith("ESG-");
        code.Split('-').Length.Should().BeGreaterThanOrEqualTo(3);
    }
}
