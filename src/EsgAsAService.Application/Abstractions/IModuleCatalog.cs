using EsgAsAService.Application.Models;

namespace EsgAsAService.Application.Abstractions;

public interface IModuleCatalog
{
    Task<IReadOnlyList<EsgModuleDefinition>> GetModulesAsync(CancellationToken ct = default);
    Task<EsgModuleDefinition?> GetModuleAsync(string sectionCode, CancellationToken ct = default);
}
