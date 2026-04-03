// 修改文件
// 路径：src/Modules/IIoT.Edge.Module.Production/Equipment/EquipmentWidget.cs
//
// 修改点：
// 1. 移除 INotificationHandler<CapacityUpdatedNotification> 实现（由 EquipmentCapacityUpdatedHandler 代理）
// 2. 移除 Handle() 方法
// 3. 构造注入由 ISender + IPlcConnectionManager + IPlcDataStore + IRecipeService + IProductionContextStore
//    改为 ISender + IRecipeService（其余依赖已移入各 Handler）
// 4. 所有数据调用改为 _sender.Send(new XxxQuery(...))
// 5. 新增 OnCapacityUpdated() 供 EquipmentCapacityUpdatedHandler 调用
// 6. Tab 结构、UI 绑定属性、DispatcherTimer 轮询逻辑完全不变

using IIoT.Edge.Contracts.Recipe;
using IIoT.Edge.Module.Production.Equipment.Models;
using IIoT.Edge.UI.Shared.PluginSystem;
using MediatR;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace IIoT.Edge.Module.Production.Equipment;

public class EquipmentWidget : WidgetBase
{
    public override string WidgetId   => "Core.Equipment";
    public override string WidgetName => "设备信息";

    private readonly ISender         _sender;
    private readonly IRecipeService  _recipeService;
    private readonly DispatcherTimer _hwRefreshTimer;

    // ── Tab 控制 ──────────────────────────────────────────────────────────
    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    // ── Tab1：硬件状态 ────────────────────────────────────────────────────
    public ObservableCollection<HardwareItemViewModel> HardwareItems { get; } = new();

    // ── Tab2：配方信息 ────────────────────────────────────────────────────
    private string _recipeName    = "暂无配方";
    private string _recipeVersion = "--";
    private string _processName   = "--";
    private bool   _isRecipeActive;

    public string RecipeName    { get => _recipeName;    set { _recipeName    = value; OnPropertyChanged(); } }
    public string RecipeVersion { get => _recipeVersion; set { _recipeVersion = value; OnPropertyChanged(); } }
    public string ProcessName   { get => _processName;   set { _processName   = value; OnPropertyChanged(); } }
    public bool   IsRecipeActive{ get => _isRecipeActive;set { _isRecipeActive = value; OnPropertyChanged(); } }

    public ObservableCollection<RecipeParamViewModel> Parameters { get; } = new();

    // ── Tab3：实时数据 ────────────────────────────────────────────────────
    private int    _todayOutput;
    private string _todayYield   = "0.00%";
    private int    _ngCount;
    private string _currentBatch = "--";

    public int    TodayOutput  { get => _todayOutput;  set { _todayOutput  = value; OnPropertyChanged(); } }
    public string TodayYield   { get => _todayYield;   set { _todayYield   = value; OnPropertyChanged(); } }
    public int    NgCount      { get => _ngCount;      set { _ngCount      = value; OnPropertyChanged(); } }
    public string CurrentBatch { get => _currentBatch; set { _currentBatch = value; OnPropertyChanged(); } }

    // ── 构造 ──────────────────────────────────────────────────────────────

    public EquipmentWidget(ISender sender, IRecipeService recipeService)
    {
        _sender        = sender;
        _recipeService = recipeService;

        LayoutRow    = 1;
        LayoutColumn = 1;

        // 配方变更事件 → 触发 Query 刷新（不直接读 recipeService 字段）
        _recipeService.RecipeChanged += RefreshRecipe;

        // 硬件状态 5 秒轮询
        _hwRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _hwRefreshTimer.Tick += async (_, _) => await RefreshHardwareAsync();

        // 作为固定右侧面板，OnActivatedAsync 不会被导航系统触发，自动在 Loaded 后初始化
        Application.Current.Dispatcher.BeginInvoke(
            System.Windows.Threading.DispatcherPriority.Loaded,
            async () => await OnActivatedAsync());
    }

    public override async Task OnActivatedAsync()
    {
        await RefreshHardwareAsync();
        RefreshRecipe();
        RefreshCapacity();
        _hwRefreshTimer.Start();
    }

    /// <summary>
    /// MediatR Notification 推送入口，由 EquipmentCapacityUpdatedHandler 调用。
    /// ViewModel 不直接实现 INotificationHandler。
    /// </summary>
    public void OnCapacityUpdated() => RefreshCapacity();

    // ── Tab1：硬件状态 ────────────────────────────────────────────────────

    private async Task RefreshHardwareAsync()
    {
        var snapshots = await _sender.Send(new GetHardwareStatusQuery());

        Application.Current.Dispatcher.Invoke(() =>
        {
            var existing = HardwareItems.ToDictionary(x => x.Name);

            foreach (var s in snapshots)
            {
                if (existing.TryGetValue(s.Name, out var vm))
                {
                    vm.Address     = s.Address;
                    vm.IsConnected = s.IsConnected;
                }
                else
                {
                    HardwareItems.Add(new HardwareItemViewModel
                    {
                        Name        = s.Name,
                        Address     = s.Address,
                        DeviceType  = s.DeviceType,
                        IsConnected = s.IsConnected
                    });
                }
            }

            var currentNames = snapshots.Select(s => s.Name).ToHashSet();
            var toRemove = HardwareItems.Where(x => !currentNames.Contains(x.Name)).ToList();
            foreach (var item in toRemove) HardwareItems.Remove(item);
        });
    }

    // ── Tab2：配方信息 ────────────────────────────────────────────────────

    private void RefreshRecipe()
    {
        _ = RefreshRecipeAsync();
    }

    private async Task RefreshRecipeAsync()
    {
        var snapshot = await _sender.Send(new GetRecipeSnapshotQuery());

        Application.Current.Dispatcher.Invoke(() =>
        {
            if (snapshot is null)
            {
                RecipeName     = "暂无配方";
                RecipeVersion  = "--";
                ProcessName    = "--";
                IsRecipeActive = false;
                Parameters.Clear();
                return;
            }

            RecipeName     = snapshot.RecipeName;
            RecipeVersion  = snapshot.RecipeVersion;
            ProcessName    = snapshot.ProcessName;
            IsRecipeActive = snapshot.IsRecipeActive;

            Parameters.Clear();
            foreach (var p in snapshot.Parameters) Parameters.Add(p);
        });
    }

    // ── Tab3：实时数据 ────────────────────────────────────────────────────

    private void RefreshCapacity()
    {
        _ = RefreshCapacityAsync();
    }

    private async Task RefreshCapacityAsync()
    {
        var snapshot = await _sender.Send(new GetCapacitySnapshotQuery());

        Application.Current.Dispatcher.Invoke(() =>
        {
            TodayOutput  = snapshot.TodayOutput;
            NgCount      = snapshot.NgCount;
            TodayYield   = snapshot.TodayYield;
            CurrentBatch = snapshot.CurrentBatch;
        });
    }
}
