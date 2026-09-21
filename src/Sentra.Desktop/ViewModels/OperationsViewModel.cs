using System.Net.Http;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sentra.Contracts.Core;
using Sentra.Contracts.Operations;
using Sentra.Desktop.Services;

namespace Sentra.Desktop.ViewModels;

public sealed partial class OperationsViewModel(
    ISentraApiClient api,
    IOfflineCacheService offline)
    : PageViewModel("Central operacional")
{
    public ObservableCollection<UnitResponse> Units { get; } = [];
    public ObservableCollection<VisitorAuthorizationResponse> Visitors { get; } = [];
    public ObservableCollection<PackageResponse> Packages { get; } = [];
    public ObservableCollection<OccurrenceResponse> Occurrences { get; } = [];
    public ObservableCollection<ShiftResponse> Shifts { get; } = [];
    public ObservableCollection<ServiceProviderResponse> Providers { get; } = [];
    public ObservableCollection<AnnouncementResponse> Announcements { get; } = [];
    public ObservableCollection<SearchResultResponse> SearchResults { get; } = [];

    [ObservableProperty] private DashboardResponse? _dashboard;
    [ObservableProperty] private UnitResponse? _selectedUnit;
    [ObservableProperty] private VisitorAuthorizationResponse? _selectedVisitor;
    [ObservableProperty] private PackageResponse? _selectedPackage;
    [ObservableProperty] private OccurrenceResponse? _selectedOccurrence;
    [ObservableProperty] private ShiftResponse? _selectedShift;

    [ObservableProperty] private string _visitorName = string.Empty;
    [ObservableProperty] private string _visitorPlate = string.Empty;
    [ObservableProperty] private string _visitorPurpose = string.Empty;
    [ObservableProperty] private string _visitorStartsAt = DateTimeOffset.Now.AddHours(1).ToString("dd/MM/yyyy HH:mm");
    [ObservableProperty] private string _visitorEndsAt = DateTimeOffset.Now.AddHours(7).ToString("dd/MM/yyyy HH:mm");

    [ObservableProperty] private string _packageCarrier = string.Empty;
    [ObservableProperty] private string _packageDescription = string.Empty;

    [ObservableProperty] private string _occurrenceCategory = "Other";
    [ObservableProperty] private string _occurrenceTitle = string.Empty;
    [ObservableProperty] private string _occurrenceDescription = string.Empty;
    [ObservableProperty] private string _occurrencePriority = "Normal";

    [ObservableProperty] private string _providerName = string.Empty;
    [ObservableProperty] private string _providerCompany = string.Empty;

    [ObservableProperty] private string _announcementTitle = string.Empty;
    [ObservableProperty] private string _announcementBody = string.Empty;

    [ObservableProperty] private string _shiftSummary = string.Empty;
    [ObservableProperty] private string _searchQuery = string.Empty;
    [ObservableProperty] private string _qrPayload = string.Empty;
    [ObservableProperty] private string _qrSvg = string.Empty;
    [ObservableProperty] private string _status = "Carregando operação...";
    [ObservableProperty] private bool _offlineMode;

    public async Task LoadAsync()
    {
        try
        {
            var units = await api.GetUnitsAsync();
            var dashboard = await api.GetDashboardAsync();
            var visitors = await api.GetVisitorAuthorizationsAsync();
            var packages = await api.GetPackagesAsync();
            var occurrences = await api.GetOccurrencesAsync();
            var shifts = await api.GetShiftsAsync();
            var providers = await api.GetProvidersAsync();
            var announcements = await api.GetAnnouncementsAsync();
            var snapshot = await api.GetOfflineSnapshotAsync();

            Replace(Units, units);
            Replace(Visitors, visitors);
            Replace(Packages, packages);
            Replace(Occurrences, occurrences);
            Replace(Shifts, shifts);
            Replace(Providers, providers);
            Replace(Announcements, announcements);
            Dashboard = dashboard;
            OfflineMode = false;
            await offline.SaveSnapshotAsync(snapshot);
            Status = "Operação sincronizada com o servidor.";
        }
        catch (HttpRequestException)
        {
            await LoadOfflineAsync();
        }
        catch (Exception exception)
        {
            Status = DescribeError(exception);
        }
    }

    [RelayCommand] private Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task CreateVisitorAsync()
    {
        if (SelectedUnit is null || string.IsNullOrWhiteSpace(VisitorName))
        {
            Status = "Selecione uma unidade e informe o nome do visitante.";
            return;
        }

        if (!TryDate(VisitorStartsAt, out var startsAt) || !TryDate(VisitorEndsAt, out var endsAt))
        {
            Status = "Datas devem estar no formato dd/MM/aaaa HH:mm.";
            return;
        }

        var request = new CreateVisitorAuthorizationRequest(
            SelectedUnit.Id, null, VisitorName.Trim(), null, null,
            startsAt, endsAt,
            string.IsNullOrWhiteSpace(VisitorPlate) ? null : VisitorPlate.Trim(),
            string.IsNullOrWhiteSpace(VisitorPurpose) ? null : VisitorPurpose.Trim());

        try
        {
            await api.CreateVisitorAuthorizationAsync(request);
            VisitorName = VisitorPlate = VisitorPurpose = string.Empty;
            await LoadAsync();
        }
        catch (HttpRequestException)
        {
            await offline.QueueAsync("POST", "api/v1/visitors/authorizations", request);
            OfflineMode = true;
            Status = "Sem conexão: autorização enfileirada localmente para sincronização.";
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private async Task RegisterEntryAsync()
    {
        if (SelectedVisitor is null) return;
        await ExecuteOrQueueAsync(
            () => api.RegisterVisitAsync(SelectedVisitor.Id, "entry"),
            "POST",
            $"api/v1/visitors/authorizations/{SelectedVisitor.Id:D}/visit",
            new RegisterVisitRequest("entry"),
            "Entrada registrada.",
            "Entrada enfileirada localmente.");
    }

    [RelayCommand]
    private async Task RegisterExitAsync()
    {
        if (SelectedVisitor is null) return;
        await ExecuteOrQueueAsync(
            () => api.RegisterVisitAsync(SelectedVisitor.Id, "exit"),
            "POST",
            $"api/v1/visitors/authorizations/{SelectedVisitor.Id:D}/visit",
            new RegisterVisitRequest("exit"),
            "Saída registrada.",
            "Saída enfileirada localmente.");
    }

    [RelayCommand]
    private async Task CreateQrAsync()
    {
        if (SelectedVisitor is null) return;
        try
        {
            var qr = await api.CreateQrAsync(SelectedVisitor.Id);
            QrPayload = qr.Payload;
            QrSvg = qr.Svg;
            Status = $"QR temporário válido até {qr.ExpiresAt:dd/MM HH:mm}.";
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private void OpenQr()
    {
        if (string.IsNullOrWhiteSpace(QrSvg))
        {
            Status = "Gere um QR primeiro.";
            return;
        }
        var path = Path.Combine(Path.GetTempPath(), $"sentra-qr-{Guid.NewGuid():N}.svg");
        File.WriteAllText(path, QrSvg);
        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
    }

    [RelayCommand]
    private async Task CreatePackageAsync()
    {
        if (SelectedUnit is null || string.IsNullOrWhiteSpace(PackageCarrier))
        {
            Status = "Selecione a unidade e informe a transportadora.";
            return;
        }

        var request = new CreatePackageRequest(
            SelectedUnit.Id,
            PackageCarrier.Trim(),
            string.IsNullOrWhiteSpace(PackageDescription) ? null : PackageDescription.Trim());

        try
        {
            await api.CreatePackageAsync(request);
            PackageCarrier = PackageDescription = string.Empty;
            await LoadAsync();
        }
        catch (HttpRequestException)
        {
            await offline.QueueAsync("POST", "api/v1/packages", request);
            OfflineMode = true;
            Status = "Encomenda enfileirada no modo offline.";
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private async Task CollectPackageAsync()
    {
        if (SelectedPackage is null) return;
        await ExecuteOrQueueAsync(
            () => api.CollectPackageAsync(SelectedPackage.Id),
            "POST",
            $"api/v1/packages/{SelectedPackage.Id:D}/collect",
            new CollectPackageRequest(null),
            "Retirada confirmada.",
            "Retirada enfileirada localmente.");
    }

    [RelayCommand]
    private async Task CreateOccurrenceAsync()
    {
        if (string.IsNullOrWhiteSpace(OccurrenceTitle) || string.IsNullOrWhiteSpace(OccurrenceDescription))
        {
            Status = "Informe título e descrição da ocorrência.";
            return;
        }

        var request = new CreateOccurrenceRequest(
            OccurrenceCategory, OccurrenceTitle.Trim(), OccurrenceDescription.Trim(),
            OccurrencePriority, SelectedUnit?.Id);

        try
        {
            await api.CreateOccurrenceAsync(request);
            OccurrenceTitle = OccurrenceDescription = string.Empty;
            await LoadAsync();
        }
        catch (HttpRequestException)
        {
            await offline.QueueAsync("POST", "api/v1/occurrences", request);
            OfflineMode = true;
            Status = "Ocorrência preservada na fila offline.";
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private async Task ResolveOccurrenceAsync()
    {
        if (SelectedOccurrence is null) return;
        try
        {
            await api.UpdateOccurrenceAsync(SelectedOccurrence.Id, new UpdateOccurrenceRequest("Resolved", null));
            await LoadAsync();
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private async Task CreateProviderAsync()
    {
        if (string.IsNullOrWhiteSpace(ProviderName)) return;
        try
        {
            await api.CreateProviderAsync(new CreateServiceProviderRequest(
                ProviderName.Trim(), null,
                string.IsNullOrWhiteSpace(ProviderCompany) ? null : ProviderCompany.Trim()));
            ProviderName = ProviderCompany = string.Empty;
            await LoadAsync();
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private async Task OpenShiftAsync()
    {
        try
        {
            SelectedShift = await api.OpenShiftAsync();
            await LoadAsync();
            Status = "Turno ativo.";
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private async Task CloseShiftAsync()
    {
        var current = Shifts.FirstOrDefault(x => x.ClosedAt is null);
        if (current is null)
        {
            Status = "Não há turno aberto.";
            return;
        }
        try
        {
            await api.CloseShiftAsync(current.Id, string.IsNullOrWhiteSpace(ShiftSummary) ? "Turno encerrado sem observações." : ShiftSummary.Trim());
            ShiftSummary = string.Empty;
            await LoadAsync();
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private async Task AcknowledgeShiftAsync()
    {
        if (SelectedShift is null) return;
        try
        {
            await api.AcknowledgeShiftAsync(SelectedShift.Id);
            await LoadAsync();
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private async Task CreateAnnouncementAsync()
    {
        if (string.IsNullOrWhiteSpace(AnnouncementTitle) || string.IsNullOrWhiteSpace(AnnouncementBody)) return;
        try
        {
            await api.CreateAnnouncementAsync(new CreateAnnouncementRequest(
                AnnouncementTitle.Trim(), AnnouncementBody.Trim(), "All",
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7)));
            AnnouncementTitle = AnnouncementBody = string.Empty;
            await LoadAsync();
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            SearchResults.Clear();
            return;
        }
        try { Replace(SearchResults, await api.SearchAsync(SearchQuery.Trim())); }
        catch (Exception e) { Status = DescribeError(e); }
    }

    private async Task LoadOfflineAsync()
    {
        var snapshot = await offline.LoadSnapshotAsync();
        if (snapshot is null)
        {
            OfflineMode = true;
            Status = "Servidor indisponível e ainda não existe cache local.";
            return;
        }

        Replace(Visitors, snapshot.ActiveAuthorizations);
        Replace(Packages, snapshot.PendingPackages);
        Replace(Providers, snapshot.Providers);
        OfflineMode = true;
        Status = $"MODO OFFLINE • cache de {snapshot.GeneratedAt:dd/MM/yyyy HH:mm}. Operações permitidas serão enfileiradas.";
    }

    private async Task ExecuteOrQueueAsync<T>(
        Func<Task<T>> online,
        string method,
        string path,
        object body,
        string onlineStatus,
        string offlineStatus)
    {
        try
        {
            await online();
            await LoadAsync();
            Status = onlineStatus;
        }
        catch (HttpRequestException)
        {
            await offline.QueueAsync(method, path, body);
            OfflineMode = true;
            Status = offlineStatus;
        }
        catch (Exception e) { Status = DescribeError(e); }
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> values)
    {
        target.Clear();
        foreach (var value in values) target.Add(value);
    }

    private static bool TryDate(string text, out DateTimeOffset value)
    {
        if (DateTime.TryParseExact(text, "dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("pt-BR"),
                DateTimeStyles.AssumeLocal, out var local))
        {
            value = new DateTimeOffset(local);
            return true;
        }
        value = default;
        return false;
    }
}
