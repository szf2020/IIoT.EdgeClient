// 修改文件
// 路径：src/Modules/IIoT.Edge.Module.Production/Monitor/MonitorWidget.cs
//
// 修改点：
// 1. 构造注入由 IProductionContextStore 改为 ISender
// 2. Refresh() 内部的数据聚合逻辑移入 GetMonitorSnapshotHandler
// 3. ViewModel 只负责调用 _sender.Send(new GetMonitorSnapshotQuery()) 并将结果填充到 DeviceTabs
// 4. DeviceTabVm 类保持在本文件末尾，内容完全不变

using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.UI.Shared.PluginSystem;
using MediatR;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows.Threading;

namespace IIoT.Edge.Module.Production.Monitor;

public class MonitorWidget : WidgetBase
{
    public override string WidgetId   => "Production.Monitor";
    public override string WidgetName => "实时数据监控";

    private readonly ISender         _sender;
    private readonly DispatcherTimer _refreshTimer;

    public ObservableCollection<DeviceTabVm> DeviceTabs { get; } = new();

    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    public MonitorWidget(ISender sender)
    {
        _sender = sender;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _refreshTimer.Tick += async (_, _) => await RefreshAsync();
        _refreshTimer.Start();
    }

    private async Task RefreshAsync()
    {
        var snapshots = await _sender.Send(new GetMonitorSnapshotQuery());

        var currentNames = snapshots.Select(s => s.DeviceName).ToHashSet();

        // 移除已不存在的设备 Tab
        var toRemove = DeviceTabs.Where(d => !currentNames.Contains(d.DeviceName)).ToList();
        foreach (var item in toRemove) DeviceTabs.Remove(item);

        // 更新或新增设备 Tab
        foreach (var s in snapshots)
        {
            var tab = DeviceTabs.FirstOrDefault(d => d.DeviceName == s.DeviceName);
            if (tab is null)
            {
                tab = new DeviceTabVm { DeviceName = s.DeviceName };
                DeviceTabs.Add(tab);
            }

            tab.DayShiftOk      = s.DayShiftOk;
            tab.DayShiftNg      = s.DayShiftNg;
            tab.DayShiftTotal   = s.DayShiftTotal;
            tab.DayShiftYield   = s.DayShiftYield;

            tab.NightShiftOk    = s.NightShiftOk;
            tab.NightShiftNg    = s.NightShiftNg;
            tab.NightShiftTotal = s.NightShiftTotal;
            tab.NightShiftYield = s.NightShiftYield;

            tab.TotalAll        = s.TotalAll;
            tab.OkAll           = s.OkAll;
            tab.NgAll           = s.NgAll;
            tab.YieldAll        = s.YieldAll;

            tab.DeviceDataSummary = s.DeviceDataSummary;
            tab.StepSummary       = s.StepSummary;
            tab.CellCount         = s.CellCount;
            tab.CellTable         = s.CellTable;
        }
    }
}

// DeviceTabVm 与原文件内容完全相同，保持在本文件末尾不动。
public class DeviceTabVm : BaseNotifyPropertyChanged
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
