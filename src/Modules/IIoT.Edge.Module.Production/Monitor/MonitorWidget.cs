using IIoT.Edge.Common.Context;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using System.Windows.Threading;

namespace IIoT.Edge.Module.Production.Monitor;

public class MonitorWidget : WidgetBase
{
    public override string WidgetId => "Production.Monitor";
    public override string WidgetName => "实时数据监控";

    private readonly IProductionContextStore _contextStore;
    private readonly DispatcherTimer _refreshTimer;

    public ObservableCollection<DeviceTabVm> DeviceTabs { get; } = new();

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    public MonitorWidget(IProductionContextStore contextStore)
    {
        _contextStore = contextStore;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += (_, _) => Refresh();
        _refreshTimer.Start();
    }

    private void Refresh()
    {
        // 1. 获取当前所有运行中的生产上下文
        var contexts = _contextStore.GetAll();

        // 2. 优化：使用 DeviceName 作为唯一标识进行同步
        var currentNames = contexts.Select(c => c.DeviceName).ToHashSet();

        // 移除已经不存在的设备 Tab
        var removeList = DeviceTabs.Where(d => !currentNames.Contains(d.DeviceName)).ToList();
        foreach (var item in removeList)
            DeviceTabs.Remove(item);

        // 3. 更新或新增设备 Tab
        foreach (var ctx in contexts)
        {
            var tab = DeviceTabs.FirstOrDefault(d => d.DeviceName == ctx.DeviceName);
            if (tab is null)
            {
                tab = new DeviceTabVm { DeviceName = ctx.DeviceName };
                DeviceTabs.Add(tab);
            }

            // ── 产能数据 ──────────────────────────────────
            var cap = ctx.TodayCapacity;

            tab.DayShiftOk = cap.DayShift.OkCount;
            tab.DayShiftNg = cap.DayShift.NgCount;
            tab.DayShiftTotal = cap.DayShift.Total;
            tab.DayShiftYield = cap.DayShift.Yield;

            tab.NightShiftOk = cap.NightShift.OkCount;
            tab.NightShiftNg = cap.NightShift.NgCount;
            tab.NightShiftTotal = cap.NightShift.Total;
            tab.NightShiftYield = cap.NightShift.Yield;

            tab.TotalAll = cap.TotalAll;
            tab.OkAll = cap.OkAll;
            tab.NgAll = cap.NgAll;
            tab.YieldAll = cap.YieldAll;

            // ── 设备数据汇总 ──────────────────────────────
            var deviceInfo = string.Join("  ",
                ctx.DeviceBag.OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}={FormatValue(kv.Value)}"));
            tab.DeviceDataSummary = string.IsNullOrEmpty(deviceInfo) ? "暂无数据" : deviceInfo;

            var stepInfo = string.Join("  ",
                ctx.StepStates.Select(kv => $"{kv.Key}={kv.Value}"));
            tab.StepSummary = string.IsNullOrEmpty(stepInfo) ? "暂无任务" : stepInfo;

            tab.CellCount = ctx.CurrentCells.Count;
            tab.CellTable = BuildCellTable(ctx);
        }
    }

    private static DataTable BuildCellTable(ProductionContext ctx)
    {
        var table = new DataTable();
        if (ctx.CurrentCells.Count == 0) return table;

        var firstCell = ctx.CurrentCells.Values.First();
        var properties = firstCell.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != nameof(CellDataBase.ProcessType)
                     && p.Name != nameof(CellDataBase.DisplayLabel))
            .ToList();

        foreach (var prop in properties)
            table.Columns.Add(prop.Name, typeof(string));

        foreach (var cell in ctx.CurrentCells.Values)
        {
            var row = table.NewRow();
            foreach (var prop in properties)
            {
                var value = prop.GetValue(cell);
                row[prop.Name] = FormatValue(value);
            }
            table.Rows.Add(row);
        }

        return table;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "--",
            DateTime dt => dt.ToString("HH:mm:ss.fff"),
            bool b => b ? "OK" : "NG",
            double d => d.ToString("F3"),
            _ => value.ToString() ?? "--"
        };
    }
}

public class DeviceTabVm : IIoT.Edge.Common.Mvvm.BaseNotifyPropertyChanged
{
    // ── 设备标识 ──────────────────────────────────
    private string _deviceName = "";
    public string DeviceName
    {
        get => _deviceName;
        set { _deviceName = value; OnPropertyChanged(); }
    }

    // ── 产能：白班 ────────────────────────────────
    private int _dayShiftOk;
    public int DayShiftOk
    {
        get => _dayShiftOk;
        set { _dayShiftOk = value; OnPropertyChanged(); }
    }

    private int _dayShiftNg;
    public int DayShiftNg
    {
        get => _dayShiftNg;
        set { _dayShiftNg = value; OnPropertyChanged(); }
    }

    private int _dayShiftTotal;
    public int DayShiftTotal
    {
        get => _dayShiftTotal;
        set { _dayShiftTotal = value; OnPropertyChanged(); }
    }

    private string _dayShiftYield = "0%";
    public string DayShiftYield
    {
        get => _dayShiftYield;
        set { _dayShiftYield = value; OnPropertyChanged(); }
    }

    // ── 产能：夜班 ────────────────────────────────
    private int _nightShiftOk;
    public int NightShiftOk
    {
        get => _nightShiftOk;
        set { _nightShiftOk = value; OnPropertyChanged(); }
    }

    private int _nightShiftNg;
    public int NightShiftNg
    {
        get => _nightShiftNg;
        set { _nightShiftNg = value; OnPropertyChanged(); }
    }

    private int _nightShiftTotal;
    public int NightShiftTotal
    {
        get => _nightShiftTotal;
        set { _nightShiftTotal = value; OnPropertyChanged(); }
    }

    private string _nightShiftYield = "0%";
    public string NightShiftYield
    {
        get => _nightShiftYield;
        set { _nightShiftYield = value; OnPropertyChanged(); }
    }

    // ── 产能：合计 ────────────────────────────────
    private int _totalAll;
    public int TotalAll
    {
        get => _totalAll;
        set { _totalAll = value; OnPropertyChanged(); }
    }

    private int _okAll;
    public int OkAll
    {
        get => _okAll;
        set { _okAll = value; OnPropertyChanged(); }
    }

    private int _ngAll;
    public int NgAll
    {
        get => _ngAll;
        set { _ngAll = value; OnPropertyChanged(); }
    }

    private string _yieldAll = "0%";
    public string YieldAll
    {
        get => _yieldAll;
        set { _yieldAll = value; OnPropertyChanged(); }
    }

    // ── 原有属性 ──────────────────────────────────
    private string _deviceDataSummary = "暂无数据";
    public string DeviceDataSummary
    {
        get => _deviceDataSummary;
        set { _deviceDataSummary = value; OnPropertyChanged(); }
    }

    private string _stepSummary = "暂无任务";
    public string StepSummary
    {
        get => _stepSummary;
        set { _stepSummary = value; OnPropertyChanged(); }
    }

    private int _cellCount;
    public int CellCount
    {
        get => _cellCount;
        set { _cellCount = value; OnPropertyChanged(); }
    }

    private DataTable? _cellTable;
    public DataTable? CellTable
    {
        get => _cellTable;
        set { _cellTable = value; OnPropertyChanged(); }
    }
}