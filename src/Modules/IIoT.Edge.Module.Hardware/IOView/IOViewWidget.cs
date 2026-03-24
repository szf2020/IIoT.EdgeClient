using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Contracts.Plc.Store;
using IIoT.Edge.Domain.Hardware.Aggregates;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;

namespace IIoT.Edge.Module.Hardware.IOView;

public class IOViewWidget : WidgetBase
{
    public override string WidgetId => "Hardware.IOView";
    public override string WidgetName => "IO交互";

    private readonly IPlcDataStore _dataStore;
    private readonly IReadRepository<NetworkDeviceEntity> _networkDevices;
    private readonly IReadRepository<IoMappingEntity> _ioMappings;
    private readonly DispatcherTimer _refreshTimer;

    // ── 设备选择 ──
    public ObservableCollection<NetworkDeviceEntity> Devices { get; }
        = new();

    private NetworkDeviceEntity? _selectedDevice;
    public NetworkDeviceEntity? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            _selectedDevice = value;
            OnPropertyChanged();
            _ = LoadMappingsAsync();
        }
    }

    // ── IO信号列表 ──
    public ObservableCollection<IoSignalVm> ReadSignals { get; }
        = new();
    public ObservableCollection<IoSignalVm> WriteSignals { get; }
        = new();

    // ── 连接状态 ──
    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; OnPropertyChanged(); }
    }

    private string _statusText = "未连接";
    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public ICommand WriteCommand { get; }
    public ICommand RefreshDevicesCommand { get; }

    public IOViewWidget(
        IPlcDataStore dataStore,
        IReadRepository<NetworkDeviceEntity> networkDevices,
        IReadRepository<IoMappingEntity> ioMappings)
    {
        _dataStore = dataStore;
        _networkDevices = networkDevices;
        _ioMappings = ioMappings;

        WriteCommand = new BaseCommand(ExecuteWrite);
        RefreshDevicesCommand =
            new AsyncCommand(LoadDevicesAsync);

        // 200ms 刷新一次读缓冲区
        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _refreshTimer.Tick += OnRefreshTick;
        _refreshTimer.Start();

        _ = LoadDevicesAsync();
    }

    private async Task LoadDevicesAsync()
    {
        var devices = await _networkDevices.GetListAsync(
            x => x.IsEnabled, CancellationToken.None);

        Devices.Clear();
        foreach (var d in devices)
            Devices.Add(d);

        if (Devices.Count > 0 && SelectedDevice is null)
            SelectedDevice = Devices[0];
    }

    private async Task LoadMappingsAsync()
    {
        ReadSignals.Clear();
        WriteSignals.Clear();

        if (SelectedDevice is null) return;

        var mappings = await _ioMappings.GetListAsync(
            x => x.NetworkDeviceId == SelectedDevice.Id,
            CancellationToken.None);

        int readIdx = 0, writeIdx = 0;
        foreach (var m in mappings.OrderBy(x => x.SortOrder))
        {
            for (int i = 0; i < m.AddressCount; i++)
            {
                var signal = new IoSignalVm
                {
                    Label = m.AddressCount == 1
                        ? m.Label
                        : $"{m.Label}[{i}]",
                    PlcAddress = m.PlcAddress,
                    Direction = m.Direction,
                    DataType = m.DataType,
                    Value = 0
                };

                if (m.Direction == "Read")
                {
                    signal.BufferIndex = readIdx++;
                    ReadSignals.Add(signal);
                }
                else
                {
                    signal.BufferIndex = writeIdx++;
                    WriteSignals.Add(signal);
                }
            }
        }

        UpdateConnectionStatus();
    }

    private void OnRefreshTick(object? sender, EventArgs e)
    {
        if (SelectedDevice is null) return;
        var buffer = _dataStore.GetBuffer(SelectedDevice.Id);
        if (buffer is null) return;

        foreach (var s in ReadSignals)
            s.Value = buffer.GetReadValue(s.BufferIndex);

        UpdateConnectionStatus();
    }

    private void ExecuteWrite(object? param)
    {
        if (SelectedDevice is null) return;
        var buffer = _dataStore.GetBuffer(SelectedDevice.Id);
        if (buffer is null) return;

        foreach (var s in WriteSignals)
            buffer.SetWriteValue(s.BufferIndex, (ushort)s.Value);
    }

    private void UpdateConnectionStatus()
    {
        if (SelectedDevice is null)
        {
            IsConnected = false;
            StatusText = "未选择设备";
            return;
        }

        var hasBuffer =
            _dataStore.HasDevice(SelectedDevice.Id);
        IsConnected = hasBuffer;
        StatusText = hasBuffer ? "已连接" : "未连接";
    }
}