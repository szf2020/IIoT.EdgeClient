using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts.Device;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Windows.Media;
using System.Windows.Threading;

namespace IIoT.Edge.UI.Shared.Widgets.Footer;

public class FooterWidget : WidgetBase
{
    public override string WidgetId => "Core.Footer";
    public override string WidgetName => "系统底栏";

    private readonly DispatcherTimer _timer;
    private readonly DateTime _startTime = DateTime.Now;
    private readonly Dispatcher _dispatcher;

    // ── 设备名称 ──────────────────────────────────────────────────
    private string _deviceName = "未识别";
    public string DeviceName
    {
        get => _deviceName;
        set { _deviceName = value; OnPropertyChanged(); }
    }

    // ── 云端连接状态文字 ──────────────────────────────────────────
    private string _cloudStatus = "未连接";
    public string CloudStatus
    {
        get => _cloudStatus;
        set { _cloudStatus = value; OnPropertyChanged(); }
    }

    // ── 云端连接状态颜色 ──────────────────────────────────────────
    private Brush _cloudStatusColor = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
    public Brush CloudStatusColor
    {
        get => _cloudStatusColor;
        set { _cloudStatusColor = value; OnPropertyChanged(); }
    }

    // ── 实时时钟 ──────────────────────────────────────────────────
    private string _currentTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
    public string CurrentTime
    {
        get => _currentTime;
        private set { _currentTime = value; OnPropertyChanged(); }
    }

    // ── 运行时长 ──────────────────────────────────────────────────
    private string _upTime = "00:00:00";
    public string UpTime
    {
        get => _upTime;
        private set { _upTime = value; OnPropertyChanged(); }
    }

    private static readonly Brush OnlineBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
    private static readonly Brush OfflineBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));

    static FooterWidget()
    {
        OnlineBrush.Freeze();
        OfflineBrush.Freeze();
    }

    public FooterWidget(IDeviceService deviceService)
    {
        LayoutRow = 2;
        LayoutColumn = 0;
        ColumnSpan = 12;

        _dispatcher = Dispatcher.CurrentDispatcher;

        // 订阅设备事件
        deviceService.DeviceIdentified += OnDeviceIdentified;
        deviceService.NetworkStateChanged += OnNetworkStateChanged;

        // 每秒刷新时钟和运行时长
        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        CurrentTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        var elapsed = DateTime.Now - _startTime;
        UpTime = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
    }

    private void OnDeviceIdentified(DeviceSession? session)
    {
        _dispatcher.Invoke(() =>
        {
            DeviceName = session?.DeviceName ?? "未识别";
        });
    }

    private void OnNetworkStateChanged(NetworkState state)
    {
        _dispatcher.Invoke(() =>
        {
            CloudStatus = state == NetworkState.Online ? "已连接" : "未连接";
            CloudStatusColor = state == NetworkState.Online ? OnlineBrush : OfflineBrush;
        });
    }
}