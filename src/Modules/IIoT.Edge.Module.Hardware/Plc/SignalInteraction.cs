// 路径：src/Modules/IIoT.Edge.Module.Hardware/Plc/SignalInteraction.cs
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Plc;
using IIoT.Edge.Contracts.Plc.Store;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Module.Hardware.Plc;

public class SignalInteraction : ISignalInteraction
{
    private readonly IPlcService _plcService;
    private readonly IPlcDataStore _dataStore;
    private readonly NetworkDeviceEntity _deviceConfig;
    private readonly ILogService _logger;

    private readonly IoMappingEntity[] _readMappings;
    private readonly IoMappingEntity[] _writeMappings;

    // 【新增】预计算：是否可以合并为一次读取/写入
    private readonly bool _canMergeRead;
    private readonly string? _mergedReadAddress;
    private readonly ushort _mergedReadCount;

    private readonly bool _canMergeWrite;
    private readonly string? _mergedWriteAddress;
    private readonly ushort _mergedWriteCount;

    private const int TaskLoopInterval = 10;
    private const int ReconnectInterval = 1000;

    public string TaskName => $"SignalInteraction_{_deviceConfig.DeviceName}";
    public bool IsConnected => _plcService.IsConnected;

    public SignalInteraction(
        IPlcService plcService,
        IPlcDataStore dataStore,
        NetworkDeviceEntity deviceConfig,
        IoMappingEntity[] ioMappings,
        ILogService logger)
    {
        _plcService = plcService;
        _dataStore = dataStore;
        _deviceConfig = deviceConfig;
        _logger = logger;

        _readMappings = ioMappings
            .Where(x => x.Direction == "Read")
            .OrderBy(x => x.SortOrder)
            .ToArray();

        _writeMappings = ioMappings
            .Where(x => x.Direction == "Write")
            .OrderBy(x => x.SortOrder)
            .ToArray();

        // 【新增】启动时判断地址是否连续，连续就合并
        (_canMergeRead, _mergedReadAddress, _mergedReadCount)
            = TryMergeMappings(_readMappings);
        (_canMergeWrite, _mergedWriteAddress, _mergedWriteCount)
            = TryMergeMappings(_writeMappings);
    }

    /// <summary>
    /// 判断映射段是否连续，如果连续则返回合并后的起始地址和总长度
    /// </summary>
    private static (bool canMerge, string? startAddress, ushort totalCount)
        TryMergeMappings(IoMappingEntity[] mappings)
    {
        if (mappings.Length == 0)
            return (false, null, 0);

        if (mappings.Length == 1)
            return (true, mappings[0].PlcAddress,
                (ushort)mappings[0].AddressCount);

        // 检查是否连续：后一段的起始地址 == 前一段起始 + 前一段长度
        // 这里假设地址格式是 "D100" 这种，解析数字部分判断连续性
        var firstNum = ParseAddressNumber(mappings[0].PlcAddress);
        if (firstNum < 0)
            return (false, null, 0);

        int expectedNext = firstNum + mappings[0].AddressCount;
        for (int i = 1; i < mappings.Length; i++)
        {
            var num = ParseAddressNumber(mappings[i].PlcAddress);
            if (num != expectedNext)
                return (false, null, 0);
            expectedNext = num + mappings[i].AddressCount;
        }

        var totalCount = (ushort)mappings.Sum(x => x.AddressCount);
        return (true, mappings[0].PlcAddress, totalCount);
    }

    /// <summary>
    /// 从PLC地址中解析数字部分，如 "D100" → 100，"W0.00" → -1（不支持合并）
    /// </summary>
    private static int ParseAddressNumber(string address)
    {
        // 取最后连续数字段
        int i = address.Length - 1;
        while (i >= 0 && char.IsDigit(address[i])) i--;

        if (i == address.Length - 1)
            return -1; // 没有数字

        return int.TryParse(address[(i + 1)..], out var num) ? num : -1;
    }

    public async Task ConnectAsync()
    {
        try
        {
            _plcService.Init(_deviceConfig.IpAddress, _deviceConfig.Port1);
            var result = await _plcService.ConnectAsync();
            if (!result)
                _logger.Warn($"[{_deviceConfig.DeviceName}] 连接失败，等待轮询重连");
        }
        catch (Exception ex)
        {
            _logger.Error($"[{_deviceConfig.DeviceName}] 连接异常: {ex.Message}");
        }
    }

    public async Task StartAsync(CancellationToken ct)
    {
        await Task.Factory.StartNew(
            () => TaskCoreAsync(ct),
            ct,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();
    }

    private async Task TaskCoreAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await DoCoreAsync();
            }
            catch (Exception ex)
            {
                _logger.Error(
                    $"[{_deviceConfig.DeviceName}] 任务循环异常: {ex.Message}");
                await Task.Delay(ReconnectInterval, ct);
            }

            await Task.Delay(TaskLoopInterval, ct);
        }
    }

    private async Task DoCoreAsync()
    {
        if (!_plcService.IsConnected)
        {
            _logger.Warn($"[{_deviceConfig.DeviceName}] 连接断开，重连中...");
            await ConnectAsync();
            return; // 重连中，本轮不读写，Buffer保持上一次的有效值
        }

        var buffer = _dataStore.GetBuffer(_deviceConfig.Id);
        if (buffer is null) return;

        // ========== 读取 ==========
        try
        {
            ushort[] allReadData;

            if (_canMergeRead && _mergedReadAddress is not null)
            {
                // 【优化】连续地址，一次读完
                var data = await _plcService.ReadDataAsync<ushort>(
                    _mergedReadAddress, _mergedReadCount);
                allReadData = data.ToArray();
            }
            else
            {
                // 非连续地址，逐段读取
                var list = new List<ushort>();
                for (int i = 0; i < _readMappings.Length; i++)
                {
                    var data = await _plcService.ReadDataAsync<ushort>(
                        _readMappings[i].PlcAddress,
                        (ushort)_readMappings[i].AddressCount);
                    list.AddRange(data);
                }
                allReadData = list.ToArray();
            }

            // 【关键】读取全部成功，才更新Buffer
            buffer.UpdateReadBuffer(allReadData);
        }
        catch (Exception ex)
        {
            // 【改动】读取失败，不更新Buffer，保持上一次有效数据
            // 状态机任务读到的还是旧值，不会误判
            _logger.Error(
                $"[{_deviceConfig.DeviceName}] 读取异常: {ex.Message}");
            _plcService.Disconnect();
            return; // 读失败就不写了，等下一轮重连
        }

        // ========== 写入 ==========
        try
        {
            var writeBuffer = buffer.GetWriteBuffer();

            if (_canMergeWrite && _mergedWriteAddress is not null)
            {
                // 【优化】连续地址，一次写完
                await _plcService.WriteDataAsync(
                    _mergedWriteAddress, writeBuffer.ToList());
            }
            else
            {
                // 非连续地址，逐段写入
                int writeOffset = 0;
                for (int i = 0; i < _writeMappings.Length; i++)
                {
                    var count = _writeMappings[i].AddressCount;
                    var segment = new ushort[count];
                    Array.Copy(writeBuffer, writeOffset, segment, 0, count);
                    await _plcService.WriteDataAsync(
                        _writeMappings[i].PlcAddress, segment.ToList());
                    writeOffset += count;
                }
            }
        }
        catch (Exception ex)
        {
            // 【改动】写入失败，不清空WriteBuffer，等重连后下一轮重新写出
            _logger.Error(
                $"[{_deviceConfig.DeviceName}] 写入异常: {ex.Message}");
            _plcService.Disconnect();
        }
    }
}