using System.Collections.Concurrent;
using EsgAsAService.Web.Models;

namespace EsgAsAService.Web.Services;

public sealed class AppState
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly DirectoryService _directoryService;
    private IReadOnlyList<OrganizationSummary> _organizations = Array.Empty<OrganizationSummary>();
    private IReadOnlyList<ReportingPeriodSummary> _periods = Array.Empty<ReportingPeriodSummary>();

    public event EventHandler<AppStateChangedEventArgs>? StateChanged;

    public OrganizationSummary? SelectedOrganization { get; private set; }
    public ReportingPeriodSummary? SelectedPeriod { get; private set; }

    public AppState(DirectoryService directoryService)
    {
        _directoryService = directoryService;
    }

    public IReadOnlyList<OrganizationSummary> Organizations => _organizations;
    public IReadOnlyList<ReportingPeriodSummary> Periods => _periods;

    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        if (_organizations.Count > 0)
        {
            return;
        }

        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_organizations.Count == 0)
            {
                _organizations = await _directoryService.GetActiveOrganizationsAsync(cancellationToken).ConfigureAwait(false);
                if (_organizations.Count > 0)
                {
                    SelectedOrganization = _organizations[0];
                    await LoadPeriodsAsync(SelectedOrganization.Id, cancellationToken).ConfigureAwait(false);
                }
            }
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public async Task SetOrganizationAsync(Guid? id, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken).ConfigureAwait(false);
        var next = id.HasValue ? _organizations.FirstOrDefault(o => o.Id == id.Value) : null;
        if (next == SelectedOrganization && next != null)
        {
            return;
        }

        SelectedOrganization = next;
        SelectedPeriod = null;

        if (SelectedOrganization != null)
        {
            await LoadPeriodsAsync(SelectedOrganization.Id, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            _periods = Array.Empty<ReportingPeriodSummary>();
        }

        PublishChange();
    }

    public async Task SetPeriodAsync(Guid? id, CancellationToken cancellationToken = default)
    {
        if (SelectedOrganization is null)
        {
            SelectedPeriod = null;
            PublishChange();
            return;
        }

        await EnsurePeriodsAsync(SelectedOrganization.Id, cancellationToken).ConfigureAwait(false);
        SelectedPeriod = id.HasValue ? _periods.FirstOrDefault(p => p.Id == id.Value) : null;
        PublishChange();
    }

    private async Task LoadPeriodsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        _periods = await _directoryService.GetReportingPeriodsAsync(organizationId, cancellationToken).ConfigureAwait(false);
        SelectedPeriod = _periods.FirstOrDefault();
    }

    private async Task EnsurePeriodsAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (_periods.Count == 0 || SelectedOrganization?.Id != organizationId)
        {
            await LoadPeriodsAsync(organizationId, cancellationToken).ConfigureAwait(false);
        }
    }

    private void PublishChange()
    {
        StateChanged?.Invoke(this, new AppStateChangedEventArgs(SelectedOrganization, SelectedPeriod));
    }
}

public sealed record AppStateChangedEventArgs(
    OrganizationSummary? Organization,
    ReportingPeriodSummary? Period);
