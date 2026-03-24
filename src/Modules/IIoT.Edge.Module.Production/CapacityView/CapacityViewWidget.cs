using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace IIoT.Edge.Module.Production.CapacityView;

public class CapacityViewWidget : WidgetBase
{
    public override string WidgetId => "Production.CapacityView";
    public override string WidgetName => "产能查询";

    private DateTime _dateFrom = DateTime.Today.AddDays(-6);
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

    // 汇总
    public int PeriodTotal { get; set; }
    public int PeriodOk { get; set; }
    public int PeriodNg { get; set; }
    public string PeriodYield { get; set; } = "";
    public string AvgDaily { get; set; } = "";

    // 每日明细
    public ObservableCollection<DailyCapacityVm> DailyRecords { get; } = new();

    public ICommand QueryCommand { get; }
    public ICommand ExportCommand { get; }

    public CapacityViewWidget()
    {
        QueryCommand = new BaseCommand(_ => LoadMockData());
        ExportCommand = new BaseCommand(_ => { });
        LoadMockData();
    }

    private void LoadMockData()
    {
        DailyRecords.Clear();
        var rnd = new Random();
        int totalAll = 0, okAll = 0, ngAll = 0;
        var days = (DateTo - DateFrom).Days + 1;

        for (int i = 0; i < days; i++)
        {
            var date = DateFrom.AddDays(i);
            var total = rnd.Next(800, 1500);
            var ng = rnd.Next(5, 30);
            var ok = total - ng;
            totalAll += total;
            okAll += ok;
            ngAll += ng;

            DailyRecords.Add(new DailyCapacityVm
            {
                Date = date.ToString("MM-dd"),
                DateFull = date.ToString("yyyy-MM-dd"),
                DayOfWeek = date.ToString("ddd"),
                Total = total,
                OkCount = ok,
                NgCount = ng,
                Yield = $"{ok * 100.0 / total:F1}%"
            });
        }

        PeriodTotal = totalAll;
        PeriodOk = okAll;
        PeriodNg = ngAll;
        PeriodYield = totalAll > 0
            ? $"{okAll * 100.0 / totalAll:F2}%"
            : "0%";
        AvgDaily = days > 0
            ? $"{totalAll / days}"
            : "0";

        OnPropertyChanged(nameof(PeriodTotal));
        OnPropertyChanged(nameof(PeriodOk));
        OnPropertyChanged(nameof(PeriodNg));
        OnPropertyChanged(nameof(PeriodYield));
        OnPropertyChanged(nameof(AvgDaily));
    }
}

public class DailyCapacityVm
{
    public string Date { get; set; } = "";
    public string DateFull { get; set; } = "";
    public string DayOfWeek { get; set; } = "";
    public int Total { get; set; }
    public int OkCount { get; set; }
    public int NgCount { get; set; }
    public string Yield { get; set; } = "";
}