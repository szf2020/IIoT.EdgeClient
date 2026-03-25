// 路径：src/Modules/IIoT.Edge.Module.Production/Monitor/MonitorWidget.cs
using IIoT.Edge.Tasks.Context;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Threading;

namespace IIoT.Edge.Module.Production.Monitor;

public class MonitorWidget : WidgetBase
{
    public override string WidgetId => "Production.Monitor";
    public override string WidgetName => "实时数据监控";

    private readonly ProductionContextStore _contextStore;
    private readonly DispatcherTimer _refreshTimer;

    /// <summary>
    /// 所有PLC设备Tab列表
    /// </summary>
    public ObservableCollection<DeviceTabVm> DeviceTabs { get; } = new();

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    public MonitorWidget(ProductionContextStore contextStore)
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

        // 增删设备Tab
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

            // 设备级数据摘要
            var deviceInfo = string.Join("  ",
                ctx.DeviceBag.OrderBy(kv => kv.Key)
                    .Select(kv => $"{kv.Key}={FormatValue(kv.Value)}"));
            tab.DeviceDataSummary = string.IsNullOrEmpty(deviceInfo) ? "暂无数据" : deviceInfo;

            // 状态机摘要
            var stepInfo = string.Join("  ",
                ctx.StepStates.Select(kv => $"{kv.Key}={kv.Value}"));
            tab.StepSummary = string.IsNullOrEmpty(stepInfo) ? "暂无任务" : stepInfo;

            // 电芯数据 → DataTable（动态列）
            tab.CellCount = ctx.CellBags.Count;
            tab.CellTable = BuildCellTable(ctx);
        }
    }

    /// <summary>
    /// 从 CellBags 构建 DataTable，行=电芯条码，列=所有Key的并集
    /// </summary>
    private static DataTable BuildCellTable(ProductionContext ctx)
    {
        var table = new DataTable();

        // 第一列固定：条码
        table.Columns.Add("条码", typeof(string));

        if (ctx.CellBags.Count == 0)
            return table;

        // 收集所有电芯的Key并集（动态列）
        var allKeys = ctx.CellBags.Values
            .SelectMany(bag => bag.Keys)
            .Distinct()
            .OrderBy(k => k)
            .ToList();

        foreach (var key in allKeys)
            table.Columns.Add(key, typeof(string));

        // 填充行
        foreach (var cell in ctx.CellBags)
        {
            var row = table.NewRow();
            row["条码"] = cell.Key;

            foreach (var kv in cell.Value)
            {
                if (table.Columns.Contains(kv.Key))
                    row[kv.Key] = FormatValue(kv.Value);
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

/// <summary>
/// 单台PLC设备的Tab展示模型
/// </summary>
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