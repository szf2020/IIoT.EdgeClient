using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Model;
using IIoT.Edge.TestSimulator.Fakes;
using IIoT.Edge.TestSimulator.Scenarios;
using IIoT.Edge.TestSimulator.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace IIoT.Edge.TestSimulator.Views;

/// <summary>场景选择项视图模型（复选框）</summary>
public sealed class ScenarioSelectionViewModel : BaseNotifyPropertyChanged
{
    private bool _isSelected;

    public string Name { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public ScenarioSelectionViewModel(string name, bool isSelected)
    {
        Name = name;
        _isSelected = isSelected;
    }
}

/// <summary>场景结果的视图模型（每张结果卡片）</summary>
public sealed class ScenarioCardViewModel : BaseNotifyPropertyChanged
{
    private bool _passed;
    private string _statusIcon = "⏳";
    private string _statusColor = "#888888";

    public string Name { get; }
    public ObservableCollection<string> AssertionLines { get; } = new();

    public bool Passed
    {
        get => _passed;
        set
        {
            _passed = value;
            StatusIcon = value ? "✅" : "❌";
            StatusColor = value ? "#2E7D32" : "#C62828";
            OnPropertyChanged();
        }
    }

    public string StatusIcon
    {
        get => _statusIcon;
        private set { _statusIcon = value; OnPropertyChanged(); }
    }

    public string StatusColor
    {
        get => _statusColor;
        private set { _statusColor = value; OnPropertyChanged(); }
    }

    public ScenarioCardViewModel(string name) => Name = name;

    public void Apply(ScenarioResult result)
    {
        AssertionLines.Clear();
        foreach (var a in result.Assertions)
            AssertionLines.Add(a.ToString());
        if (result.Error != null)
            AssertionLines.Add($"⚠ {result.Error}");
        Passed = result.Passed;
    }

    public void Reset()
    {
        AssertionLines.Clear();
        StatusIcon = "⏳";
        StatusColor = "#888888";
    }
}

/// <summary>主窗口 ViewModel</summary>
public sealed class MainWindowViewModel : BaseNotifyPropertyChanged
{
    private readonly ScenarioRunner _runner;
    private readonly FakeLogService _logService;
    private bool _isBusy;
    private bool _resetBeforeRun = true;

    public ObservableCollection<string> LogMessages { get; } = new();
    public ObservableCollection<ScenarioCardViewModel> ScenarioCards { get; } = new();
    public ObservableCollection<ScenarioSelectionViewModel> ScenarioSelections { get; } = new();

    public ICommand RunAllCommand { get; }
    public ICommand RunSelectedCommand { get; }
    public ICommand RunOfflineOnlyCommand { get; }
    public ICommand RunHistoricalCommand { get; }
    public ICommand ResetCommand { get; }

    public bool IsBusy
    {
        get => _isBusy;
        private set { _isBusy = value; OnPropertyChanged(); }
    }

    public bool ResetBeforeRun
    {
        get => _resetBeforeRun;
        set { _resetBeforeRun = value; OnPropertyChanged(); }
    }

    // 历史数据场景名称常量
    private const string HistoricalScenarioName = "历史数据生成（2026-01 至 2026-04-02）";

    public MainWindowViewModel(ScenarioRunner runner, FakeLogService logService)
    {
        _runner = runner;
        _logService = logService;

        // ── 场景卡片（显示结果用）────────────────────────────────
        ScenarioCards.Add(new ScenarioCardViewModel("场景一：在线正常上报"));
        ScenarioCards.Add(new ScenarioCardViewModel("场景二：离线落库"));
        ScenarioCards.Add(new ScenarioCardViewModel("场景三：恢复补传"));
        ScenarioCards.Add(new ScenarioCardViewModel(HistoricalScenarioName));

        // ── 场景勾选列表 ──────────────────────────────────────────
        ScenarioSelections.Add(new ScenarioSelectionViewModel("场景一：在线正常上报", true));
        ScenarioSelections.Add(new ScenarioSelectionViewModel("场景二：离线落库", true));
        ScenarioSelections.Add(new ScenarioSelectionViewModel("场景三：恢复补传", true));
        ScenarioSelections.Add(new ScenarioSelectionViewModel(HistoricalScenarioName, false));

        logService.EntryAdded += OnLogAdded;

        RunAllCommand = new AsyncCommand<object>(async _ => await RunAllAsync());
        RunSelectedCommand = new AsyncCommand<object>(async _ => await RunSelectedAsync());
        RunOfflineOnlyCommand = new AsyncCommand<object>(async _ => await RunOfflineOnlyAsync());
        RunHistoricalCommand = new AsyncCommand<object>(async _ => await RunHistoricalAsync());
        ResetCommand = new AsyncCommand<object>(async _ => await ResetAsync());
    }

    private void OnLogAdded(LogEntry entry)
    {
        var text = $"[{entry.Time:HH:mm:ss}] [{entry.Level,-5}] {entry.Message}";
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
            dispatcher.Invoke(() => LogMessages.Add(text));
        else
            LogMessages.Add(text);
    }

    private Task RunAllAsync()
    {
        foreach (var item in ScenarioSelections)
            item.IsSelected = item.Name != HistoricalScenarioName; // 运行全部不包含历史数据场景

        return RunSelectedAsync();
    }

    private Task RunOfflineOnlyAsync()
    {
        foreach (var item in ScenarioSelections)
            item.IsSelected = item.Name == "场景二：离线落库";

        return RunSelectedAsync();
    }

    /// <summary>单独运行历史数据生成，不清库，不影响其他场景</summary>
    private async Task RunHistoricalAsync()
    {
        if (IsBusy) return;

        var confirm = MessageBox.Show(
            "将生成 2026-01-01 至 2026-04-02 约55万条电芯记录写入 SQLite。\n\n是否继续？",
            "历史数据生成确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        IsBusy = true;
        LogMessages.Clear();

        var card = ScenarioCards.FirstOrDefault(c => c.Name == HistoricalScenarioName);
        card?.Reset();

        try
        {
            var results = await Task.Run(() =>
                _runner.RunSelectedAsync(
                    new[] { HistoricalScenarioName },
                    resetBeforeRun: false)); // 历史数据不清库

            if (results.Count > 0 && card != null)
                Application.Current?.Dispatcher.Invoke(() => card.Apply(results[0]));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task RunSelectedAsync()
    {
        if (IsBusy) return;

        var selectedNames = ScenarioSelections
            .Where(x => x.IsSelected)
            .Select(x => x.Name)
            .ToList();

        if (selectedNames.Count == 0)
        {
            MessageBox.Show("请至少勾选一个场景", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        IsBusy = true;
        LogMessages.Clear();

        foreach (var card in ScenarioCards)
            card.Reset();

        try
        {
            var results = await Task.Run(() =>
                _runner.RunSelectedAsync(selectedNames, ResetBeforeRun));

            var cardMap = ScenarioCards.ToDictionary(x => x.Name, x => x);
            foreach (var result in results)
            {
                if (!cardMap.TryGetValue(result.Name, out var card)) continue;
                Application.Current?.Dispatcher.Invoke(() => card.Apply(result));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ResetAsync()
    {
        if (IsBusy) return;
        LogMessages.Clear();
        foreach (var card in ScenarioCards)
            card.Reset();
        await _runner.ResetAsync();
    }
}