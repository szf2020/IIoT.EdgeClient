using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IIoT.Edge.Module.Formula.RecipeView;

public class RecipeViewWidget : WidgetBase
{
    public override string WidgetId
        => "Formula.RecipeView";
    public override string WidgetName
        => "产品配方";

    public ObservableCollection<RecipeVm>
        Recipes
    { get; } = new();

    private RecipeVm? _selectedRecipe;
    public RecipeVm? SelectedRecipe
    {
        get => _selectedRecipe;
        set { _selectedRecipe = value; OnPropertyChanged(); }
    }

    public ICommand RefreshCommand { get; }

    public RecipeViewWidget()
    {
        RefreshCommand = new AsyncCommand(async () =>
        {
            // 后期接 CloudSync 拉取
            await Task.Delay(300);
            LoadMockData();
        });

        LoadMockData();
    }

    private void LoadMockData()
    {
        Recipes.Clear();

        var r1 = new RecipeVm
        {
            Name = "冬季特调工艺",
            Version = "V1.2",
            Process = "叠片工序",
            UpdatedAt = "2026-03-20 14:30",
            Status = "已启用"
        };
        r1.Params.Add(new RecipeParamVm
        {
            Name = "切刀速度",
            Value = "120",
            Unit = "mm/s"
        });
        r1.Params.Add(new RecipeParamVm
        {
            Name = "张力值",
            Value = "0.8",
            Unit = "N"
        });
        r1.Params.Add(new RecipeParamVm
        {
            Name = "温度",
            Value = "25",
            Unit = "℃"
        });
        r1.Params.Add(new RecipeParamVm
        {
            Name = "对齐精度",
            Value = "0.05",
            Unit = "mm"
        });

        var r2 = new RecipeVm
        {
            Name = "夏季标准工艺",
            Version = "V2.0",
            Process = "叠片工序",
            UpdatedAt = "2026-03-18 09:15",
            Status = "待审核"
        };
        r2.Params.Add(new RecipeParamVm
        {
            Name = "切刀速度",
            Value = "130",
            Unit = "mm/s"
        });
        r2.Params.Add(new RecipeParamVm
        {
            Name = "张力值",
            Value = "0.9",
            Unit = "N"
        });
        r2.Params.Add(new RecipeParamVm
        {
            Name = "温度",
            Value = "22",
            Unit = "℃"
        });

        var r3 = new RecipeVm
        {
            Name = "高速测试工艺",
            Version = "V0.1",
            Process = "叠片工序",
            UpdatedAt = "2026-03-22 16:00",
            Status = "已停用"
        };
        r3.Params.Add(new RecipeParamVm
        {
            Name = "切刀速度",
            Value = "180",
            Unit = "mm/s"
        });

        Recipes.Add(r1);
        Recipes.Add(r2);
        Recipes.Add(r3);
        SelectedRecipe = r1;
    }
}

public class RecipeVm : BaseNotifyPropertyChanged
{
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Process { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
    public string Status { get; set; } = "";
    public ObservableCollection<RecipeParamVm>
        Params
    { get; } = new();
}

public class RecipeParamVm
{
    public string Name { get; set; } = "";
    public string Value { get; set; } = "";
    public string Unit { get; set; } = "";
}