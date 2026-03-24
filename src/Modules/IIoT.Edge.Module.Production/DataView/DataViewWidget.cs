using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IIoT.Edge.Module.Production.DataView;

public class DataViewWidget : WidgetBase
{
    public override string WidgetId
        => "Production.DataView";
    public override string WidgetName
        => "生产数据";

    // ── 汇总数据 ──
    public int TodayTotal { get; set; } = 1234;
    public int TodayOk { get; set; } = 1216;
    public int TodayNg { get; set; } = 18;
    public string TodayYield { get; set; } = "98.54%";

    // ── 明细列表 ──
    public ObservableCollection<ProductionRecordVm>
        Records
    { get; } = new();

    // ── 筛选 ──
    private DateTime _dateFrom = DateTime.Today;
    public DateTime DateFrom
    {
        get => _dateFrom;
        set { _dateFrom = value; OnPropertyChanged(); }
    }

    private DateTime _dateTo = DateTime.Today;
    public DateTime DateTo
    {
        get => _dateTo;
        set { _dateTo = value; OnPropertyChanged(); }
    }

    public ICommand QueryCommand { get; }
    public ICommand ExportCommand { get; }

    public DataViewWidget()
    {
        QueryCommand = new BaseCommand(_ =>
            LoadMockData());
        ExportCommand = new BaseCommand(_ =>
        {
            // 后期接 Excel 导出
        });

        LoadMockData();
    }

    private void LoadMockData()
    {
        Records.Clear();
        var rnd = new Random();
        for (int i = 0; i < 30; i++)
        {
            var time = DateTime.Today
                .AddHours(8).AddMinutes(i * 15);
            var total = rnd.Next(30, 60);
            var ng = rnd.Next(0, 3);
            Records.Add(new ProductionRecordVm
            {
                Time = time.ToString("HH:mm"),
                BatchNo = $"LOT-{DateTime.Today:yyyyMMdd}"
                    + $"-{i + 1:D3}",
                Total = total,
                OkCount = total - ng,
                NgCount = ng,
                Yield = $"{(total - ng) * 100.0
                    / total:F1}%"
            });
        }
    }
}

public class ProductionRecordVm
{
    public string Time { get; set; } = "";
    public string BatchNo { get; set; } = "";
    public int Total { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public string Yield { get; set; } = "";
}