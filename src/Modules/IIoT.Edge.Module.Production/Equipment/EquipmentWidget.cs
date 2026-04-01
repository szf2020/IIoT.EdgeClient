// 路径：src/Modules/IIoT.Edge.Module.Production/Equipment/EquipmentWidget.cs
using IIoT.Edge.Contracts.Context;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Contracts.Events;
using IIoT.Edge.Contracts.Hardware.Queries;
using IIoT.Edge.Contracts.Plc;
using IIoT.Edge.Contracts.Plc.Store;
using IIoT.Edge.Contracts.Recipe;
using IIoT.Edge.Module.Production.Equipment.Models;
using IIoT.Edge.UI.Shared.PluginSystem;
using MediatR;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace IIoT.Edge.Module.Production.Equipment;

public class EquipmentWidget : WidgetBase, INotificationHandler<CapacityUpdatedNotification>
{
    public override string WidgetId => "Core.Equipment";
    public override string WidgetName => "设备信息";

    private readonly ISender _sender;
    private readonly IPlcConnectionManager _plcManager;
    private readonly IPlcDataStore _dataStore;
    private readonly IRecipeService _recipeService;
    private readonly IProductionContextStore _contextStore;
    private readonly DispatcherTimer _hwRefreshTimer;

    // ── Tab 控制 ──────────────────────────────────────────────────
    private int _selectedTabIndex;
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set { _selectedTabIndex = value; OnPropertyChanged(); }
    }

    // ── Tab1：硬件状态 ────────────────────────────────────────────
    public ObservableCollection<HardwareItemViewModel> HardwareItems { get; } = new();

    // ── Tab2：配方信息 ────────────────────────────────────────────
    private string _recipeName = "暂无配方";
    public string RecipeName
    {
        get => _recipeName;
        set { _recipeName = value; OnPropertyChanged(); }
    }

    private string _recipeVersion = "--";
    public string RecipeVersion
    {
        get => _recipeVersion;
        set { _recipeVersion = value; OnPropertyChanged(); }
    }

    private string _processName = "--";
    public string ProcessName
    {
        get => _processName;
        set { _processName = value; OnPropertyChanged(); }
    }

    private bool _isRecipeActive;
    public bool IsRecipeActive
    {
        get => _isRecipeActive;
        set { _isRecipeActive = value; OnPropertyChanged(); }
    }

    public ObservableCollection<RecipeParamViewModel> Parameters { get; } = new();

    // ── Tab3：实时数据 ────────────────────────────────────────────
    private int _todayOutput;
    public int TodayOutput
    {
        get => _todayOutput;
        set { _todayOutput = value; OnPropertyChanged(); }
    }

    private string _todayYield = "0.00%";
    public string TodayYield
    {
        get => _todayYield;
        set { _todayYield = value; OnPropertyChanged(); }
    }

    private int _ngCount;
    public int NgCount
    {
        get => _ngCount;
        set { _ngCount = value; OnPropertyChanged(); }
    }

    private string _currentBatch = "--";
    public string CurrentBatch
    {
        get => _currentBatch;
        set { _currentBatch = value; OnPropertyChanged(); }
    }

    public EquipmentWidget(
        ISender sender,
        IPlcConnectionManager plcManager,
        IPlcDataStore dataStore,
        IRecipeService recipeService,
        IProductionContextStore contextStore)
    {
        _sender = sender;
        _plcManager = plcManager;
        _dataStore = dataStore;
        _recipeService = recipeService;
        _contextStore = contextStore;

        LayoutRow = 1;
        LayoutColumn = 1;

        // 配方变更时实时刷新 Tab2
        _recipeService.RecipeChanged += RefreshRecipe;

        // 硬件状态 5 秒轮询（连接状态变化低频）
        _hwRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _hwRefreshTimer.Tick += async (_, _) => await RefreshHardwareAsync();

        // 作为固定右侧面板，OnActivatedAsync 不会被导航系统触发
        // 在 UI 线程空闲后自动执行一次初始化
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

    // ── Tab1：硬件状态 ────────────────────────────────────────────

    private async Task RefreshHardwareAsync()
    {
        var result = await _sender.Send(new GetAllNetworkDevicesQuery());
        if (!result.IsSuccess || result.Value is null) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            // 同步更新：保留已有条目避免闪烁
            var existing = HardwareItems.ToDictionary(x => x.Name);

            foreach (var device in result.Value)
            {
                // 双重判断：PLC 实例已连接 或 Buffer 已注册（任一为真即视为在线）
                var isConnected = (_plcManager.GetPlc(device.Id)?.IsConnected == true)
                               || _dataStore.HasDevice(device.Id);

                if (existing.TryGetValue(device.DeviceName, out var vm))
                {
                    vm.Address = device.IpAddress;
                    vm.IsConnected = isConnected;
                }
                else
                {
                    HardwareItems.Add(new HardwareItemViewModel
                    {
                        Name = device.DeviceName,
                        Address = device.IpAddress,
                        DeviceType = device.DeviceType.ToString(),
                        IsConnected = isConnected
                    });
                }
            }

            // 移除已不存在的设备
            var currentNames = result.Value.Select(d => d.DeviceName).ToHashSet();
            var toRemove = HardwareItems.Where(x => !currentNames.Contains(x.Name)).ToList();
            foreach (var item in toRemove) HardwareItems.Remove(item);
        });
    }

    // ── Tab2：配方信息 ────────────────────────────────────────────

    private void RefreshRecipe()
    {
        // 优先云端配方，云端为空时 fallback 到本地配方，都没有才显示"暂无"
        var recipe = _recipeService.CloudRecipe
                  ?? _recipeService.LocalRecipe
                  ?? _recipeService.ActiveRecipe;

        if (recipe is null)
        {
            RecipeName = "暂无配方";
            RecipeVersion = "--";
            ProcessName = "--";
            IsRecipeActive = false;
            Application.Current.Dispatcher.Invoke(() => Parameters.Clear());
            return;
        }

        var sourceTag = recipe == _recipeService.CloudRecipe ? "云端"
                      : recipe == _recipeService.LocalRecipe ? "本地" : "";

        RecipeName = string.IsNullOrEmpty(sourceTag)
            ? recipe.RecipeName
            : $"{recipe.RecipeName}（{sourceTag}）";
        RecipeVersion = recipe.Version;
        ProcessName = recipe.ProcessName;
        IsRecipeActive = recipe.Status == "Active";

        Application.Current.Dispatcher.Invoke(() =>
        {
            Parameters.Clear();
            foreach (var p in recipe.Parameters.Values)
            {
                Parameters.Add(new RecipeParamViewModel
                {
                    ParamName = p.Name,
                    CurrentValue = p.CustomValue ?? "--",
                    MinValue = p.Min?.ToString("G4") ?? "--",
                    MaxValue = p.Max?.ToString("G4") ?? "--",
                    Unit = p.Unit,
                    WarnLow = p.Min.HasValue ? (p.Min * 1.05)?.ToString("G4") ?? "--" : "--",
                    WarnHigh = p.Max.HasValue ? (p.Max * 0.95)?.ToString("G4") ?? "--" : "--"
                });
            }
        });
    }

    // ── Tab3：实时数据（CapacityUpdatedNotification 驱动）────────

    /// <summary>
    /// MediatR 通知：每次电芯完成时 CapacityConsumer 发布，自动推送到此
    /// </summary>
    public Task Handle(CapacityUpdatedNotification notification, CancellationToken ct)
    {
        RefreshCapacity();
        return Task.CompletedTask;
    }

    private void RefreshCapacity()
    {
        var contexts = _contextStore.GetAll();

        int totalOk = 0, totalNg = 0;
        string lastBatch = "--";

        foreach (var ctx in contexts)
        {
            totalOk += ctx.TodayCapacity.OkAll;
            totalNg += ctx.TodayCapacity.NgAll;

            if (ctx.DeviceBag.TryGetValue("CurrentBatch", out var b) && b is string batchStr)
                lastBatch = batchStr;
        }

        var totalAll = totalOk + totalNg;

        Application.Current.Dispatcher.Invoke(() =>
        {
            TodayOutput = totalAll;
            NgCount = totalNg;
            TodayYield = totalAll > 0 ? $"{totalOk * 100.0 / totalAll:F1}%" : "0.0%";
            CurrentBatch = lastBatch;
        });
    }
}