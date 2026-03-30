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
            // 优化：按 DeviceName 查找
            var tab = DeviceTabs.FirstOrDefault(d => d.DeviceName == ctx.DeviceName);
            if (tab is null)
            {
                tab = new DeviceTabVm { DeviceName = ctx.DeviceName };
                DeviceTabs.Add(tab);
            }

            // 更新汇总信息
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

        // 通过反射获取电芯属性作为列名
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
    // 优化：移除 DeviceId，统一使用 DeviceName
    private string _deviceName = "";
    public string DeviceName
    {
        get => _deviceName;
        set { _deviceName = value; OnPropertyChanged(); }
    }

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