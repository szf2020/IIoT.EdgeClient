// 修改文件
// 路径：src/Modules/IIoT.Edge.Module.Formula/RecipeView/RecipeViewWidget.cs
//
// 修改点：
// 1. 构造注入改为 ISender + IRecipeService（IRecipeService 只用于订阅 RecipeChanged 事件）
// 2. 所有对 recipeService 方法的直接调用，改为 _sender.Send(new XxxCommand/Query(...))
// 3. 其余 UI 属性、命令定义、RecipeParamVm 完全不变

using IIoT.Edge.Common.DataPipeline.Recipe;
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Recipe;
using IIoT.Edge.UI.Shared.PluginSystem;
using MediatR;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace IIoT.Edge.Module.Formula.RecipeView;

public class RecipeViewWidget : WidgetBase
{
    public override string WidgetId   => "Formula.RecipeView";
    public override string WidgetName => "产品配方";

    private readonly ISender        _sender;
    private readonly IRecipeService _recipeService; // 仅用于订阅事件

    // ── 配方参数列表 ─────────────────────────────
    public ObservableCollection<RecipeParamVm> Params { get; } = new();

    // ── 配方元信息 ───────────────────────────────
    private string _recipeName    = "";
    private string _recipeVersion = "";
    private string _processName   = "";
    private string _updatedAt     = "";

    public string RecipeName    { get => _recipeName;    set { _recipeName    = value; OnPropertyChanged(); } }
    public string RecipeVersion { get => _recipeVersion; set { _recipeVersion = value; OnPropertyChanged(); } }
    public string ProcessName   { get => _processName;   set { _processName   = value; OnPropertyChanged(); } }
    public string UpdatedAt     { get => _updatedAt;     set { _updatedAt     = value; OnPropertyChanged(); } }

    // ── 数据源切换 ───────────────────────────────
    private bool _isCloudSource = true;
    public bool IsCloudSource
    {
        get => _isCloudSource;
        set
        {
            _isCloudSource = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SourceLabel));
        }
    }
    public string SourceLabel => IsCloudSource ? "云端配方" : "本地配方";

    private bool _isLocalAdmin;
    public bool IsLocalAdmin
    {
        get => _isLocalAdmin;
        set { _isLocalAdmin = value; OnPropertyChanged(); }
    }

    // ── 本地编辑 ─────────────────────────────────
    private string _editKey  = "";
    private string _editMin  = "";
    private string _editMax  = "";
    private string _editUnit = "";

    public string EditKey  { get => _editKey;  set { _editKey  = value; OnPropertyChanged(); } }
    public string EditMin  { get => _editMin;  set { _editMin  = value; OnPropertyChanged(); } }
    public string EditMax  { get => _editMax;  set { _editMax  = value; OnPropertyChanged(); } }
    public string EditUnit { get => _editUnit; set { _editUnit = value; OnPropertyChanged(); } }

    // ── 命令 ─────────────────────────────────────
    public ICommand SyncCloudCommand       { get; }
    public ICommand SwitchSourceCommand    { get; }
    public ICommand SaveLocalParamCommand  { get; }
    public ICommand DeleteLocalParamCommand{ get; }

    public RecipeViewWidget(ISender sender, IRecipeService recipeService)
    {
        _sender        = sender;
        _recipeService = recipeService;

        SyncCloudCommand        = new AsyncCommand(OnSyncCloudAsync);
        SwitchSourceCommand     = new BaseCommand(_ => OnSwitchSource());
        SaveLocalParamCommand   = new BaseCommand(_ => OnSaveLocalParam());
        DeleteLocalParamCommand = new BaseCommand(OnDeleteLocalParam);

        _recipeService.RecipeChanged += RefreshUI;
        _recipeService.RecipeChanged += () => _ = UpdateAdminStateAsync();
    }

    public override async Task OnActivatedAsync()
    {
        await UpdateAdminStateAsync();
        IsCloudSource = _recipeService.ActiveSource == RecipeSource.Cloud;
        RefreshUI();
    }

    // ── 命令实现 ─────────────────────────────────

    private async Task OnSyncCloudAsync()
    {
        var success = await _sender.Send(new SyncRecipeFromCloudCommand());
        if (!success)
        {
            MessageBox.Show("配方拉取失败，请检查网络连接。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnSwitchSource()
    {
        var newSource = IsCloudSource ? RecipeSource.Local : RecipeSource.Cloud;
        _ = _sender.Send(new SwitchRecipeSourceCommand(newSource));
        IsCloudSource = newSource == RecipeSource.Cloud;
    }

    private void OnSaveLocalParam()
    {
        if (string.IsNullOrWhiteSpace(EditKey)) return;

        double? min = double.TryParse(EditMin, out var minVal) ? minVal : null;
        double? max = double.TryParse(EditMax, out var maxVal) ? maxVal : null;

        _ = _sender.Send(new SaveLocalRecipeParamCommand(
            EditKey.Trim(), min, max, EditUnit.Trim()));

        EditKey = ""; EditMin = ""; EditMax = ""; EditUnit = "";
    }

    private void OnDeleteLocalParam(object? param)
    {
        if (param is string key && !string.IsNullOrEmpty(key))
            _ = _sender.Send(new DeleteLocalRecipeParamCommand(key));
    }

    // ── 内部方法 ─────────────────────────────────

    private async Task UpdateAdminStateAsync()
    {
        IsLocalAdmin = await _sender.Send(new GetIsLocalAdminQuery());
    }

    private void RefreshUI()
    {
        _ = RefreshUIAsync();
    }

    private async Task RefreshUIAsync()
    {
        var snapshot = await _sender.Send(new GetRecipeViewSnapshotQuery());

        if (snapshot is null)
        {
            RecipeName    = "未加载";
            RecipeVersion = ""; ProcessName = ""; UpdatedAt = "";
            Params.Clear();
            return;
        }

        RecipeName    = snapshot.RecipeName;
        RecipeVersion = snapshot.RecipeVersion;
        ProcessName   = snapshot.ProcessName;
        UpdatedAt     = snapshot.UpdatedAt;
        IsCloudSource = snapshot.IsCloudSource;

        Params.Clear();
        foreach (var p in snapshot.Params) Params.Add(p);
    }
}

// RecipeParamVm 与原文件内容完全相同，保持在本文件末尾不动。
public class RecipeParamVm
{
    public string Name { get; set; } = "";
    public string Min  { get; set; } = "";
    public string Max  { get; set; } = "";
    public string Unit { get; set; } = "";
}
