using System.Threading;
using System.Threading.Tasks;
using EsgAsAService.Application.Models;
using EsgAsAService.Domain.Entities;

namespace EsgAsAService.Application.Abstractions;

public interface IFullReportComposer
{
    Task<FullEsgReport> ComposeAsync(Company companyV1, ReportingPeriod periodV1, CancellationToken ct = default);
}

