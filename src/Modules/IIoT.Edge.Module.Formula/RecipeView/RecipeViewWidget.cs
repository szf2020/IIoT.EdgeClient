using IIoT.Edge.Common.DataPipeline.Recipe;
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Auth;
using IIoT.Edge.Contracts.Recipe;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace IIoT.Edge.Module.Formula.RecipeView;

public class RecipeViewWidget : WidgetBase
{
    public override string WidgetId => "Formula.RecipeView";
    public override string WidgetName => "产品配方";

    private readonly IRecipeService _recipeService;
    private readonly IAuthService _authService;

    // ── 配方参数列表 ─────────────────────────────

    public ObservableCollection<RecipeParamVm> Params { get; } = new();

    // ── 配方元信息 ───────────────────────────────

    private string _recipeName = "";
    public string RecipeName
    {
        get => _recipeName;
        set { _recipeName = value; OnPropertyChanged(); }
    }

    private string _recipeVersion = "";
    public string RecipeVersion
    {
        get => _recipeVersion;
        set { _recipeVersion = value; OnPropertyChanged(); }
    }

    private string _processName = "";
    public string ProcessName
    {
        get => _processName;
        set { _processName = value; OnPropertyChanged(); }
    }

    private string _updatedAt = "";
    public string UpdatedAt
    {
        get => _updatedAt;
        set { _updatedAt = value; OnPropertyChanged(); }
    }

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

    private string _editKey = "";
    public string EditKey
    {
        get => _editKey;
        set { _editKey = value; OnPropertyChanged(); }
    }

    private string _editMin = "";
    public string EditMin
    {
        get => _editMin;
        set { _editMin = value; OnPropertyChanged(); }
    }

    private string _editMax = "";
    public string EditMax
    {
        get => _editMax;
        set { _editMax = value; OnPropertyChanged(); }
    }

    private string _editUnit = "";
    public string EditUnit
    {
        get => _editUnit;
        set { _editUnit = value; OnPropertyChanged(); }
    }

    // ── 命令 ─────────────────────────────────────

    public ICommand SyncCloudCommand { get; }
    public ICommand SwitchSourceCommand { get; }
    public ICommand SaveLocalParamCommand { get; }
    public ICommand DeleteLocalParamCommand { get; }

    public RecipeViewWidget(
        IRecipeService recipeService,
        IAuthService authService)
    {
        _recipeService = recipeService;
        _authService = authService;

        SyncCloudCommand = new AsyncCommand(OnSyncCloudAsync);
        SwitchSourceCommand = new BaseCommand(_ => OnSwitchSource());
        SaveLocalParamCommand = new BaseCommand(_ => OnSaveLocalParam());
        DeleteLocalParamCommand = new BaseCommand(OnDeleteLocalParam);

        _recipeService.RecipeChanged += RefreshUI;
        _authService.AuthStateChanged += _ => UpdateAdminState();

        UpdateAdminState();
        IsCloudSource = _recipeService.ActiveSource == RecipeSource.Cloud;
        RefreshUI();
    }

    public override Task OnActivatedAsync()
    {
        UpdateAdminState();
        RefreshUI();
        return Task.CompletedTask;
    }

    // ── 命令实现 ─────────────────────────────────

    private async Task OnSyncCloudAsync()
    {
        var success = await _recipeService.PullFromCloudAsync();
        if (!success)
        {
            MessageBox.Show("配方拉取失败，请检查网络连接。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnSwitchSource()
    {
        var newSource = IsCloudSource ? RecipeSource.Local : RecipeSource.Cloud;
        _recipeService.SwitchSource(newSource);
        IsCloudSource = newSource == RecipeSource.Cloud;
    }

    private void OnSaveLocalParam()
    {
        if (string.IsNullOrWhiteSpace(EditKey)) return;

        double? min = double.TryParse(EditMin, out var minVal) ? minVal : null;
        double? max = double.TryParse(EditMax, out var maxVal) ? maxVal : null;

        _recipeService.SetLocalParam(EditKey.Trim(), min, max, EditUnit.Trim());
        EditKey = "";
        EditMin = "";
        EditMax = "";
        EditUnit = "";
    }

    private void OnDeleteLocalParam(object? param)
    {
        if (param is string key && !string.IsNullOrEmpty(key))
            _recipeService.RemoveLocalParam(key);
    }

    // ── 内部方法 ─────────────────────────────────

    private void UpdateAdminState()
    {
        IsLocalAdmin = _authService.CurrentUser?.IsLocalAdmin ?? false;
    }

    private void RefreshUI()
    {
        var recipe = _recipeService.ActiveRecipe;

        if (recipe is null)
        {
            RecipeName = "未加载";
            RecipeVersion = "";
            ProcessName = "";
            UpdatedAt = "";
            Params.Clear();
            return;
        }

        RecipeName = recipe.RecipeName;
        RecipeVersion = recipe.Version;
        ProcessName = recipe.ProcessName;
        UpdatedAt = recipe.UpdatedAt;

        Params.Clear();
        foreach (var kv in recipe.Parameters.OrderBy(kv => kv.Key))
        {
            var p = kv.Value;
            Params.Add(new RecipeParamVm
            {
                Name = p.Name,
                Min = p.Min?.ToString("F2") ?? "--",
                Max = p.Max?.ToString("F2") ?? "--",
                Unit = p.Unit
            });
        }
    }
}

public class RecipeParamVm
{
    public string Name { get; set; } = "";
    public string Min { get; set; } = "";
    public string Max { get; set; } = "";
    public string Unit { get; set; } = "";
}