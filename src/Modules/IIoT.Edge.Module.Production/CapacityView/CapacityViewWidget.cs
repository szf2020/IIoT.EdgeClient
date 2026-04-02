using IIoT.Edge.Common.DataPipeline.Capacity;
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace IIoT.Edge.Module.Production.CapacityView;

public class CapacityViewWidget : WidgetBase
{
    public override string WidgetId => "Production.CapacityView";
    public override string WidgetName => "产能查询";

    private readonly IProductionContextStore _contextStore;
    private readonly IDeviceService _deviceService;
    private readonly CapacityCloudQueryService _queryService;

    public ObservableCollection<string> DeviceNames { get; } = new();
    public ObservableCollection<string> QueryModes { get; } = new() { "按日查询", "按月查询", "按年查询" };
    public ObservableCollection<DailyCapacityVm> DailyRecords { get; } = new();
    public ObservableCollection<CapacityChartBarVm> ChartBars { get; } = new();

    private string _selectedDeviceName = "";
    public string SelectedDeviceName
    {
        get => _selectedDeviceName;
        set { _selectedDeviceName = value; OnPropertyChanged(); _ = LoadCurrentDataAsync(); }
    }

    private string _selectedQueryMode = "按日查询";
    public string SelectedQueryMode
    {
        get => _selectedQueryMode;
        set { _selectedQueryMode = value; OnPropertyChanged(); }
    }

    private DateTime _queryDate = DateTime.Today;
    public DateTime QueryDate
    {
        get => _queryDate;
        set { _queryDate = value; OnPropertyChanged(); }
    }

    private bool _isOnline;
    public bool IsOnline
    {
        get => _isOnline;
        set
        {
            _isOnline = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanQueryCloud));
            OnPropertyChanged(nameof(OfflineHint));
        }
    }

    public bool CanQueryCloud => IsOnline;
    public string OfflineHint => IsOnline ? "" : "云端不通，仅显示当前产能";

    private int _periodTotal;
    private int _periodOk;
    private int _periodNg;
    private string _periodYield = "0%";
    private string _avgDaily = "0";

    public int PeriodTotal { get => _periodTotal; set { _periodTotal = value; OnPropertyChanged(); } }
    public int PeriodOk { get => _periodOk; set { _periodOk = value; OnPropertyChanged(); } }
    public int PeriodNg { get => _periodNg; set { _periodNg = value; OnPropertyChanged(); } }
    public string PeriodYield { get => _periodYield; set { _periodYield = value; OnPropertyChanged(); } }
    public string AvgDaily { get => _avgDaily; set { _avgDaily = value; OnPropertyChanged(); } }

    public ICommand QueryCommand { get; }
    public ICommand ExportCommand { get; }

    public CapacityViewWidget(
        IProductionContextStore contextStore,
        IDeviceService deviceService,
        CapacityCloudQueryService queryService)
    {
        _contextStore = contextStore;
        _deviceService = deviceService;
        _queryService = queryService;

        QueryCommand = new AsyncCommand(QueryHistoryAsync);
        ExportCommand = new BaseCommand(_ => { });

        _deviceService.NetworkStateChanged += OnNetworkStateChanged;
    }

    public override async Task OnActivatedAsync()
    {
        RefreshDeviceList();
        IsOnline = _deviceService.CurrentState == NetworkState.Online;
        await LoadCurrentDataAsync();
    }

    private void OnNetworkStateChanged(NetworkState state)
    {
        IsOnline = state == NetworkState.Online;
        _ = LoadCurrentDataAsync();
    }

    private void RefreshDeviceList()
    {
        var contexts = _contextStore.GetAll();
        var names = contexts.Select(c => c.DeviceName).OrderBy(n => n).ToList();

        DeviceNames.Clear();
        foreach (var name in names) DeviceNames.Add(name);

        if (!string.IsNullOrEmpty(_selectedDeviceName) && names.Contains(_selectedDeviceName))
            return;

        _selectedDeviceName = names.FirstOrDefault() ?? "";
        OnPropertyChanged(nameof(SelectedDeviceName));
    }

    // ── 获取当前设备 DeviceId ────────────────────────────────────────────

    private Guid? GetCurrentDeviceId()
        => _deviceService.CurrentDevice?.DeviceId;

    // ── 启动时加载当天数据 ───────────────────────────────────────────────

    private async Task LoadCurrentDataAsync()
    {
        DailyRecords.Clear();
        if (string.IsNullOrEmpty(_selectedDeviceName) || !CanQueryCloud)
        {
            ClearSummary();
            RefreshChart();
            return;
        }

        var deviceId = GetCurrentDeviceId();
        if (deviceId is null) { ClearSummary(); RefreshChart(); return; }

        var productionDate = _queryService.GetProductionDate(DateTime.Now);
        var rows = await _queryService.QueryByProductionDayAsync(deviceId.Value, productionDate);

        ApplyRows(rows);
        AvgDaily = $"{PeriodTotal}";
        RefreshChart();
    }

    // ── 历史查询 ─────────────────────────────────────────────────────────

    private async Task QueryHistoryAsync()
    {
        if (string.IsNullOrEmpty(_selectedDeviceName))
        { ClearSummary(); return; }

        if (!CanQueryCloud)
        {
            MessageBox.Show("云端不通，无法查询产能数据。", "产能查询",
                MessageBoxButton.OK, MessageBoxImage.Information);
            ClearSummary();
            DailyRecords.Clear();
            RefreshChart();
            return;
        }

        var deviceId = GetCurrentDeviceId();
        if (deviceId is null) { ClearSummary(); return; }

        List<DailyCapacityVm> rows;

        if (SelectedQueryMode == "按月查询")
            rows = await _queryService.QueryByMonthAsync(deviceId.Value, QueryDate.Year, QueryDate.Month);
        else if (SelectedQueryMode == "按年查询")
            rows = await _queryService.QueryByYearAsync(deviceId.Value, QueryDate.Year);
        else
            rows = await _queryService.QueryByProductionDayAsync(deviceId.Value, QueryDate.Date);

        ApplyRows(rows);

        var divisor = SelectedQueryMode == "按年查询" ? 12 : Math.Max(1, rows.Count);
        AvgDaily = $"{PeriodTotal / divisor}";
        RefreshChart();
    }

    // ── 工具方法 ─────────────────────────────────────────────────────────

    private void ApplyRows(List<DailyCapacityVm> rows)
    {
        DailyRecords.Clear();
        foreach (var row in rows) DailyRecords.Add(row);

        PeriodTotal = rows.Sum(x => x.Total);
        PeriodOk = rows.Sum(x => x.OkCount);
        PeriodNg = rows.Sum(x => x.NgCount);
        PeriodYield = PeriodTotal > 0
            ? $"{PeriodOk * 100.0 / PeriodTotal:F2}%"
            : "0%";
    }

    private void RefreshChart()
    {
        ChartBars.Clear();
        var max = DailyRecords.Count > 0 ? DailyRecords.Max(x => x.Total) : 0;
        var safeMax = max <= 0 ? 1 : max;

        foreach (var row in DailyRecords)
        {
            var ratio = row.Total * 1.0 / safeMax;
            ChartBars.Add(new CapacityChartBarVm
            {
                Label = row.DateFull,
                Value = row.Total,
                HeightRatio = ratio,
                ChartHeight = Math.Max(2, ratio * 190)
            });
        }
    }

    private void ClearSummary()
    {
        PeriodTotal = 0;
        PeriodOk = 0;
        PeriodNg = 0;
        PeriodYield = "0%";
        AvgDaily = "0";
    }
}