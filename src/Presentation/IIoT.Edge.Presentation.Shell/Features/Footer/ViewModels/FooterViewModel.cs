using IIoT.Edge.Application.Abstractions.Device;
using IIoT.Edge.UI.Shared.Mvvm;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Windows.Media;
using System.Windows.Threading;

namespace IIoT.Edge.Presentation.Shell.Features.Footer;

public class FooterViewModel : ViewModelBase
{
    private readonly DispatcherTimer _timer;
    private readonly DateTime _startTime = DateTime.Now;
    private readonly Dispatcher _dispatcher;
    private string _deviceName = "Unknown";
    private string _cloudStatus = "Offline";
    private Brush _cloudStatusColor = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));
    private string _currentTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
    private string _upTime = "00:00:00";

    private static readonly Brush OnlineBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
    private static readonly Brush OfflineBrush = new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44));

    public override string ViewId => "Core.Footer";
    public override string ViewTitle => "Footer";

    public string DeviceName
    {
        get => _deviceName;
        set { _deviceName = value; OnPropertyChanged(); }
    }

    public string CloudStatus
    {
        get => _cloudStatus;
        set { _cloudStatus = value; OnPropertyChanged(); }
    }

    public Brush CloudStatusColor
    {
        get => _cloudStatusColor;
        set { _cloudStatusColor = value; OnPropertyChanged(); }
    }

    public string CurrentTime
    {
        get => _currentTime;
        private set { _currentTime = value; OnPropertyChanged(); }
    }

    public string UpTime
    {
        get => _upTime;
        private set { _upTime = value; OnPropertyChanged(); }
    }

    static FooterViewModel()
    {
        OnlineBrush.Freeze();
        OfflineBrush.Freeze();
    }

    public FooterViewModel(IDeviceService deviceService)
    {
        LayoutRow = 2;
        LayoutColumn = 0;
        ColumnSpan = 12;

        _dispatcher = Dispatcher.CurrentDispatcher;

        deviceService.DeviceIdentified += OnDeviceIdentified;
        deviceService.NetworkStateChanged += OnNetworkStateChanged;

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
            DeviceName = session?.DeviceName ?? "Unknown";
        });
    }

    private void OnNetworkStateChanged(NetworkState state)
    {
        _dispatcher.Invoke(() =>
        {
            CloudStatus = state == NetworkState.Online ? "Online" : "Offline";
            CloudStatusColor = state == NetworkState.Online ? OnlineBrush : OfflineBrush;
        });
    }
}
