using System.IO;

namespace EsgAsAService.Infrastructure.Factors;

public sealed class FactorRepositoryOptions
{
    public string RootPath { get; set; } = Path.Combine("data", "factors");
}
