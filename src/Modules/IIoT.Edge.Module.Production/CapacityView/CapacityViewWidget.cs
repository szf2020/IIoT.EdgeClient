using IIoT.Edge.Common.DataPipeline.Capacity;
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;

namespace IIoT.Edge.Module.Production.CapacityView;

public class CapacityViewWidget : WidgetBase
{
    public override string WidgetId => "Production.CapacityView";
    public override string WidgetName => "产能查询";

    private readonly IProductionContextStore _contextStore;
    private readonly IDeviceService _deviceService;
    private readonly ICloudHttpClient _cloudHttpClient;
    private readonly ShiftConfig _shiftConfig;

    public ObservableCollection<string> DeviceNames { get; } = new();
    public ObservableCollection<string> QueryModes { get; } = new() { "按日查询", "按月查询", "按年查询" };

    private string _selectedDeviceName = "";
    public string SelectedDeviceName
    {
        get => _selectedDeviceName;
        set
        {
            _selectedDeviceName = value;
            OnPropertyChanged();
            _ = LoadCurrentDataAsync();
        }
    }

    private string _selectedQueryMode = "按日查询";
    public string SelectedQueryMode
    {
        get => _selectedQueryMode;
        set
        {
            _selectedQueryMode = value;
            OnPropertyChanged();
        }
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
    public int PeriodTotal { get => _periodTotal; set { _periodTotal = value; OnPropertyChanged(); } }

    private int _periodOk;
    public int PeriodOk { get => _periodOk; set { _periodOk = value; OnPropertyChanged(); } }

    private int _periodNg;
    public int PeriodNg { get => _periodNg; set { _periodNg = value; OnPropertyChanged(); } }

    private string _periodYield = "0%";
    public string PeriodYield { get => _periodYield; set { _periodYield = value; OnPropertyChanged(); } }

    private string _avgDaily = "0";
    public string AvgDaily { get => _avgDaily; set { _avgDaily = value; OnPropertyChanged(); } }

    public ObservableCollection<DailyCapacityVm> DailyRecords { get; } = new();
    public ObservableCollection<CapacityChartBarVm> ChartBars { get; } = new();

    public ICommand QueryCommand { get; }
    public ICommand ExportCommand { get; }

    public CapacityViewWidget(
        IProductionContextStore contextStore,
        IDeviceService deviceService,
        ICloudHttpClient cloudHttpClient,
        ShiftConfig shiftConfig)
    {
        _contextStore = contextStore;
        _deviceService = deviceService;
        _cloudHttpClient = cloudHttpClient;
        _shiftConfig = shiftConfig;

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
        foreach (var name in names)
            DeviceNames.Add(name);

        if (!string.IsNullOrEmpty(_selectedDeviceName) && names.Contains(_selectedDeviceName))
            return;

        _selectedDeviceName = names.FirstOrDefault() ?? "";
        OnPropertyChanged(nameof(SelectedDeviceName));
    }

    private async Task LoadCurrentDataAsync()
    {
        DailyRecords.Clear();

        if (string.IsNullOrEmpty(_selectedDeviceName))
        {
            ClearSummary();
            RefreshChart();
            return;
        }

        if (!CanQueryCloud)
        {
            ClearSummary();
            RefreshChart();
            return;
        }

        var rows = await QueryByDayAsync(_selectedDeviceName, DateTime.Today);

        DailyRecords.Clear();
        foreach (var row in rows)
            DailyRecords.Add(row);

        var total = rows.Sum(x => x.Total);
        var ok = rows.Sum(x => x.OkCount);
        var ng = rows.Sum(x => x.NgCount);

        PeriodTotal = total;
        PeriodOk = ok;
        PeriodNg = ng;
        PeriodYield = total > 0 ? $"{ok * 100.0 / total:F2}%" : "0%";
        AvgDaily = $"{total}";

        RefreshChart();
    }

    private async Task QueryHistoryAsync()
    {
        if (string.IsNullOrEmpty(_selectedDeviceName))
        {
            ClearSummary();
            return;
        }

        if (!CanQueryCloud)
        {
            MessageBox.Show("云端不通，无法查询产能数据。", "产能查询", MessageBoxButton.OK, MessageBoxImage.Information);
            ClearSummary();
            DailyRecords.Clear();
            RefreshChart();
            return;
        }

        List<DailyCapacityVm> rows;

        if (SelectedQueryMode == "按月查询")
            rows = await QueryByMonthAsync(_selectedDeviceName, QueryDate.Year, QueryDate.Month);
        else if (SelectedQueryMode == "按年查询")
            rows = await QueryByYearAsync(_selectedDeviceName, QueryDate.Year);
        else
            rows = await QueryByDayAsync(_selectedDeviceName, QueryDate.Date);

        DailyRecords.Clear();
        foreach (var row in rows)
            DailyRecords.Add(row);

        var total = rows.Sum(x => x.Total);
        var ok = rows.Sum(x => x.OkCount);
        var ng = rows.Sum(x => x.NgCount);

        PeriodTotal = total;
        PeriodOk = ok;
        PeriodNg = ng;
        PeriodYield = total > 0 ? $"{ok * 100.0 / total:F2}%" : "0%";

        var divisor = SelectedQueryMode == "按年查询" ? 12 : Math.Max(1, rows.Count);
        AvgDaily = divisor > 0 ? $"{total / divisor}" : "0";

        RefreshChart();
    }

    private async Task<List<DailyCapacityVm>> QueryByDayAsync(string deviceName, DateTime date)
    {
        var hourly = await QueryCloudHourlyAsync(deviceName, date);
        if (hourly.Count > 0)
            return hourly;

        var summary = await QueryCloudDailyAsync(deviceName, date);
        if (summary is not null)
            return new List<DailyCapacityVm> { summary };

        return new List<DailyCapacityVm>();
    }

    private async Task<List<DailyCapacityVm>> QueryByMonthAsync(string deviceName, int year, int month)
    {
        var result = new List<DailyCapacityVm>();
        var firstDay = new DateTime(year, month, 1);
        var days = DateTime.DaysInMonth(year, month);

        for (int i = 0; i < days; i++)
        {
            var date = firstDay.AddDays(i);
            var vm = await QueryCloudDailyAsync(deviceName, date);
            if (vm is null) continue;
            result.Add(vm);
        }

        return result;
    }

    private async Task<List<DailyCapacityVm>> QueryByYearAsync(string deviceName, int year)
    {
        var result = new List<DailyCapacityVm>();

        for (int month = 1; month <= 12; month++)
        {
            var monthRows = await QueryByMonthAsync(deviceName, year, month);
            var total = monthRows.Sum(x => x.Total);
            var ok = monthRows.Sum(x => x.OkCount);
            var ng = monthRows.Sum(x => x.NgCount);

            result.Add(new DailyCapacityVm
            {
                Date = $"{year}-{month:D2}",
                DateFull = $"{year}-{month:D2}",
                DayOfWeek = "--",
                Total = total,
                OkCount = ok,
                NgCount = ng,
                Yield = total > 0 ? $"{ok * 100.0 / total:F1}%" : "0%"
            });
        }

        return result;
    }

    private async Task<List<DailyCapacityVm>> QueryCloudHourlyAsync(string deviceName, DateTime date)
    {
        var url = $"/api/v1/Capacity/hourly?deviceName={Uri.EscapeDataString(deviceName)}&date={date:yyyy-MM-dd}";
        var json = await _cloudHttpClient.GetAsync(url);
        if (string.IsNullOrWhiteSpace(json))
            return new List<DailyCapacityVm>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Array)
                return new List<DailyCapacityVm>();

            var result = new List<DailyCapacityVm>();
            foreach (var item in root.EnumerateArray())
            {
                var hour = ReadInt(item, "hour", "Hour");
                var minute = ReadInt(item, "minute", "Minute", "minuteBucket", "MinuteBucket");
                var total = ReadInt(item, "totalCount", "total", "Total");
                var ok = ReadInt(item, "okCount", "ok", "OkCount");
                var ng = ReadInt(item, "ngCount", "ng", "NgCount");
                var shift = ReadString(item, "shiftCode", "ShiftCode");
                var label = ReadString(item, "timeLabel", "TimeLabel");
                if (string.IsNullOrWhiteSpace(label))
                {
                    var endMinute = minute == 30 ? 0 : 30;
                    var endHour = minute == 30 ? (hour + 1) % 24 : hour;
                    label = $"{hour:D2}:{minute:D2}-{endHour:D2}:{endMinute:D2}";
                }

                result.Add(new DailyCapacityVm
                {
                    Date = date.ToString("MM-dd"),
                    DateFull = label,
                    DayOfWeek = string.IsNullOrWhiteSpace(shift) ? GetShiftCodeByTime(hour, minute) : shift,
                    Total = total,
                    OkCount = ok,
                    NgCount = ng,
                    Yield = total > 0 ? $"{ok * 100.0 / total:F1}%" : "0%"
                });
            }

            return result.OrderBy(x => x.DateFull).ToList();
        }
        catch
        {
            return new List<DailyCapacityVm>();
        }
    }

    private async Task<DailyCapacityVm?> QueryCloudDailyAsync(string deviceName, DateTime date)
    {
        var url = $"/api/v1/Capacity/summary?deviceName={Uri.EscapeDataString(deviceName)}&date={date:yyyy-MM-dd}";
        var json = await _cloudHttpClient.GetAsync(url);
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                if (root.GetArrayLength() == 0) return null;
                root = root[0];
            }

            var total = ReadInt(root, "totalCount", "total", "Total");
            var ok = ReadInt(root, "okCount", "ok", "OkCount");
            var ng = ReadInt(root, "ngCount", "ng", "NgCount");

            var dayTotal = ReadInt(root, "dayShiftTotal", "dayTotal", "DayShiftTotal");
            var dayOk = ReadInt(root, "dayShiftOk", "dayOk", "DayShiftOk");
            var dayNg = ReadInt(root, "dayShiftNg", "dayNg", "DayShiftNg");

            var nightTotal = ReadInt(root, "nightShiftTotal", "nightTotal", "NightShiftTotal");
            var nightOk = ReadInt(root, "nightShiftOk", "nightOk", "NightShiftOk");
            var nightNg = ReadInt(root, "nightShiftNg", "nightNg", "NightShiftNg");

            if (total == 0)
                total = dayTotal + nightTotal;
            if (ok == 0 && ng == 0)
            {
                ok = dayOk + nightOk;
                ng = dayNg + nightNg;
            }

            return new DailyCapacityVm
            {
                Date = date.ToString("MM-dd"),
                DateFull = date.ToString("yyyy-MM-dd"),
                DayOfWeek = date.ToString("ddd"),
                Total = total,
                OkCount = ok,
                NgCount = ng,
                Yield = total > 0 ? $"{ok * 100.0 / total:F1}%" : "0%",
                DayShiftTotal = dayTotal,
                DayShiftOk = dayOk,
                DayShiftNg = dayNg,
                NightShiftTotal = nightTotal,
                NightShiftOk = nightOk,
                NightShiftNg = nightNg,
            };
        }
        catch
        {
            return null;
        }
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

    private string GetShiftCodeByTime(int hour, int minute)
    {
        var t = new TimeSpan(hour, minute, 0);
        var isDay = t >= _shiftConfig.DayStartTime && t < _shiftConfig.DayEndTime;
        return isDay ? "D" : "N";
    }

    private static int ReadInt(JsonElement root, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!root.TryGetProperty(key, out var prop))
                continue;

            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n))
                return n;

            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var s))
                return s;
        }

        return 0;
    }

    private static string ReadString(JsonElement root, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!root.TryGetProperty(key, out var prop))
                continue;

            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString() ?? string.Empty;
        }

        return string.Empty;
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

public class DailyCapacityVm
{
    public string Date { get; set; } = "";
    public string DateFull { get; set; } = "";
    public string DayOfWeek { get; set; } = "";
    public int Total { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public string Yield { get; set; } = "";

    public int DayShiftTotal { get; set; }
    public int DayShiftOk { get; set; }
    public int DayShiftNg { get; set; }
    public int NightShiftTotal { get; set; }
    public int NightShiftOk { get; set; }
    public int NightShiftNg { get; set; }
}

public class CapacityChartBarVm
{
    public string Label { get; set; } = "";
    public int Value { get; set; }
    public double HeightRatio { get; set; }
    public double ChartHeight { get; set; }
}