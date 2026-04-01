using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IIoT.Edge.Module.Production.CapacityView;

public class CapacityViewWidget : WidgetBase
{
    public override string WidgetId => "Production.CapacityView";
    public override string WidgetName => "产能查询";

    private readonly ITodayCapacityStore _capacityStore;
    private readonly IProductionContextStore _contextStore;

    // ── 筛选条件 ──────────────────────────────────

    public ObservableCollection<string> DeviceNames { get; } = new();

    private string _selectedDeviceName = "";
    public string SelectedDeviceName
    {
        get => _selectedDeviceName;
        set
        {
            _selectedDeviceName = value;
            OnPropertyChanged();
            LoadData();
        }
    }

    private DateTime _dateFrom = DateTime.Today;
    public DateTime DateFrom
    {
        get => _dateFrom;
        set { _dateFrom = value; OnPropertyChanged(); }
    }

    private DateTime _dateTo = DateTime.Today;
    public DateTime DateTo
    {
        get => _dateTo;
        set { _dateTo = value; OnPropertyChanged(); }
    }

    // ── 汇总 ─────────────────────────────────────

    private int _periodTotal;
    public int PeriodTotal
    {
        get => _periodTotal;
        set { _periodTotal = value; OnPropertyChanged(); }
    }

    private int _periodOk;
    public int PeriodOk
    {
        get => _periodOk;
        set { _periodOk = value; OnPropertyChanged(); }
    }

    private int _periodNg;
    public int PeriodNg
    {
        get => _periodNg;
        set { _periodNg = value; OnPropertyChanged(); }
    }

    private string _periodYield = "";
    public string PeriodYield
    {
        get => _periodYield;
        set { _periodYield = value; OnPropertyChanged(); }
    }

    private string _avgDaily = "";
    public string AvgDaily
    {
        get => _avgDaily;
        set { _avgDaily = value; OnPropertyChanged(); }
    }

    // ── 每日明细 ──────────────────────────────────

    public ObservableCollection<DailyCapacityVm> DailyRecords { get; } = new();

    // ── 命令 ─────────────────────────────────────

    public ICommand QueryCommand { get; }
    public ICommand ExportCommand { get; }

    public CapacityViewWidget(
        ITodayCapacityStore capacityStore,
        IProductionContextStore contextStore)
    {
        _capacityStore = capacityStore;
        _contextStore = contextStore;

        QueryCommand = new BaseCommand(_ => LoadData());
        ExportCommand = new BaseCommand(_ => { /* TODO: 导出 Excel */ });
    }

    /// <summary>
    /// 页面激活时刷新设备列表 + 加载数据
    /// </summary>
    public override Task OnActivatedAsync()
    {
        RefreshDeviceList();
        LoadData();
        return Task.CompletedTask;
    }

    /// <summary>
    /// 从 ProductionContextStore 刷新设备下拉列表
    /// </summary>
    private void RefreshDeviceList()
    {
        var contexts = _contextStore.GetAll();
        var names = contexts.Select(c => c.DeviceName).OrderBy(n => n).ToList();

        DeviceNames.Clear();
        foreach (var name in names)
            DeviceNames.Add(name);

        // 如果当前选中项不在列表中，自动选第一个
        if (!string.IsNullOrEmpty(_selectedDeviceName) && names.Contains(_selectedDeviceName))
            return;

        // 直接设字段避免触发 setter 里的 LoadData（下面会调）
        _selectedDeviceName = names.FirstOrDefault() ?? "";
        OnPropertyChanged(nameof(SelectedDeviceName));
    }

    /// <summary>
    /// 加载产能数据
    /// 当天：从内存 TodayCapacityStore 读
    /// 历史：TODO 走云端 API
    /// </summary>
    private void LoadData()
    {
        DailyRecords.Clear();

        if (string.IsNullOrEmpty(_selectedDeviceName))
        {
            ClearSummary();
            return;
        }

        int totalAll = 0, okAll = 0, ngAll = 0;
        var days = (DateTo - DateFrom).Days + 1;
        if (days <= 0)
        {
            ClearSummary();
            return;
        }

        for (int i = 0; i < days; i++)
        {
            var date = DateFrom.AddDays(i);

            if (date.Date == DateTime.Today)
            {
                // ── 当天：从内存读 ──────────────────
                var snapshot = _capacityStore.GetSnapshot(_selectedDeviceName);
                var total = snapshot.TotalAll;
                var ok = snapshot.OkAll;
                var ng = snapshot.NgAll;
                totalAll += total;
                okAll += ok;
                ngAll += ng;

                DailyRecords.Add(new DailyCapacityVm
                {
                    Date = date.ToString("MM-dd"),
                    DateFull = date.ToString("yyyy-MM-dd"),
                    DayOfWeek = date.ToString("ddd"),
                    Total = total,
                    OkCount = ok,
                    NgCount = ng,
                    Yield = total > 0 ? $"{ok * 100.0 / total:F1}%" : "0%",
                    DayShiftTotal = snapshot.DayShift.Total,
                    DayShiftOk = snapshot.DayShift.OkCount,
                    DayShiftNg = snapshot.DayShift.NgCount,
                    NightShiftTotal = snapshot.NightShift.Total,
                    NightShiftOk = snapshot.NightShift.OkCount,
                    NightShiftNg = snapshot.NightShift.NgCount,
                });
            }
            else
            {
                // ── 历史：TODO 走云端 API ────────────
                // GET /api/v1/Capacity/summary?deviceName={}&date={}
                DailyRecords.Add(new DailyCapacityVm
                {
                    Date = date.ToString("MM-dd"),
                    DateFull = date.ToString("yyyy-MM-dd"),
                    DayOfWeek = date.ToString("ddd"),
                    Total = 0,
                    OkCount = 0,
                    NgCount = 0,
                    Yield = "--",
                    DayShiftTotal = 0,
                    DayShiftOk = 0,
                    DayShiftNg = 0,
                    NightShiftTotal = 0,
                    NightShiftOk = 0,
                    NightShiftNg = 0,
                });
            }
        }

        // ── 汇总 ────────────────────────────────
        PeriodTotal = totalAll;
        PeriodOk = okAll;
        PeriodNg = ngAll;
        PeriodYield = totalAll > 0
            ? $"{okAll * 100.0 / totalAll:F2}%"
            : "0%";
        AvgDaily = days > 0
            ? $"{totalAll / days}"
            : "0";
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

    // 班次明细
    public int DayShiftTotal { get; set; }
    public int DayShiftOk { get; set; }
    public int DayShiftNg { get; set; }
    public int NightShiftTotal { get; set; }
    public int NightShiftOk { get; set; }
    public int NightShiftNg { get; set; }
}