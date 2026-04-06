using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.UI.Shared.PluginSystem;
using MediatR;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace IIoT.Edge.Module.Production.CapacityView;

public class CapacityViewWidget : WidgetBase
{
    public override string WidgetId => "Production.CapacityView";
    public override string WidgetName => "产能查询";

    private readonly ISender _sender;
    private readonly IProductionContextStore _contextStore;
    private readonly IDeviceService _deviceService;

    // DeviceNames 来自本地 PLC 上下文，下拉选哪台 PLC 就查那台的云端产能
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
        ISender sender,
        IProductionContextStore contextStore,
        IDeviceService deviceService)
    {
        _sender = sender;
        _contextStore = contextStore;
        _deviceService = deviceService;

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

    /// <summary>由 CapacityViewUpdatedHandler 调用，不直接实现 INotificationHandler</summary>
    public void OnCapacityUpdated() => _ = LoadCurrentDataAsync();

    private void OnNetworkStateChanged(NetworkState state)
    {
        IsOnline = state == NetworkState.Online;
        _ = LoadCurrentDataAsync();
    }

    private void RefreshDeviceList()
    {
        var names = _contextStore.GetAll()
            .Select(c => c.DeviceName)
            .OrderBy(n => n)
            .ToList();

        DeviceNames.Clear();
        foreach (var name in names) DeviceNames.Add(name);

        if (!string.IsNullOrEmpty(_selectedDeviceName) && names.Contains(_selectedDeviceName))
            return;

        _selectedDeviceName = names.FirstOrDefault() ?? "";
        OnPropertyChanged(nameof(SelectedDeviceName));
    }

    // cloudDeviceId 来自 MAC 寻址，固定唯一；plcName 来自下拉，区分同一上位机多台 PLC
    private Guid? GetCloudDeviceId()
        => _deviceService.CurrentDevice?.DeviceId;

    private async Task LoadCurrentDataAsync()
    {
        DailyRecords.Clear();
        if (!CanQueryCloud)
        {
            ClearSummary();
            RefreshChart();
            return;
        }

        var deviceId = GetCloudDeviceId();
        if (deviceId is null) { ClearSummary(); RefreshChart(); return; }

        var result = await _sender.Send(
            new LoadTodayCapacityQuery(deviceId.Value, DateTime.Now, _selectedDeviceName));

        ApplyResult(result);
    }

    private async Task QueryHistoryAsync()
    {
        if (!CanQueryCloud)
        {
            MessageBox.Show("云端不通，无法查询产能数据。", "产能查询",
                MessageBoxButton.OK, MessageBoxImage.Information);
            ClearSummary();
            DailyRecords.Clear();
            RefreshChart();
            return;
        }

        var deviceId = GetCloudDeviceId();
        if (deviceId is null) { ClearSummary(); return; }

        var result = await _sender.Send(
            new QueryCapacityHistoryQuery(
                deviceId.Value, SelectedQueryMode, QueryDate, _selectedDeviceName));

        ApplyResult(result);
    }

    private void ApplyResult(CapacityViewResult result)
    {
        DailyRecords.Clear();
        foreach (var row in result.Rows) DailyRecords.Add(row);

        PeriodTotal = result.PeriodTotal;
        PeriodOk = result.PeriodOk;
        PeriodNg = result.PeriodNg;
        PeriodYield = result.PeriodYield;
        AvgDaily = result.AvgDaily;

        RefreshChart();
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
        PeriodTotal = 0; PeriodOk = 0; PeriodNg = 0;
        PeriodYield = "0%"; AvgDaily = "0";
    }
}