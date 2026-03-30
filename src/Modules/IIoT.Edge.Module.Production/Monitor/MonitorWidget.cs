using IIoT.Edge.Common.Context;
using IIoT.Edge.Common.DataPipeline.CellData;
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Tasks.Context;
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
        var contexts = _contextStore.GetAll();

        var currentIds = contexts.Select(c => c.DeviceId).ToHashSet();
        var removeList = DeviceTabs.Where(d => !currentIds.Contains(d.DeviceId)).ToList();
        foreach (var item in removeList)
            DeviceTabs.Remove(item);

        foreach (var ctx in contexts)
        {
            var tab = DeviceTabs.FirstOrDefault(d => d.DeviceId == ctx.DeviceId);
            if (tab is null)
            {
                tab = new DeviceTabVm { DeviceId = ctx.DeviceId };
                DeviceTabs.Add(tab);
            }

            tab.DeviceName = ctx.DeviceName;

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

    /// <summary>
    /// 从 CurrentCells 构建 DataTable
    /// 强类型属性自动变成列，不用再手动收集 key
    /// </summary>
    private static DataTable BuildCellTable(ProductionContext ctx)
    {
        var table = new DataTable();

        if (ctx.CurrentCells.Count == 0)
            return table;

        // 取第一颗电芯的类型，用反射获取所有属性作为列
        var firstCell = ctx.CurrentCells.Values.First();
        var properties = firstCell.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != nameof(CellDataBase.ProcessType)
                     && p.Name != nameof(CellDataBase.DisplayLabel))
            .ToList();

        foreach (var prop in properties)
            table.Columns.Add(prop.Name, typeof(string));

        // 填充行
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
    public int DeviceId { get; set; }

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