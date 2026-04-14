using IIoT.Edge.Application.Abstractions.Plc;
using S7.Net;
using PlcClient = S7.Net.Plc;

namespace IIoT.Edge.Infrastructure.DeviceComm.Plc.Services;

public class S7PlcService : IPlcService, IDisposable
{
    private PlcClient _plc = null!;
    private string _ip = string.Empty;
    private int _port;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public bool IsConnected => _plc?.IsConnected ?? false;

    public void Init(string ip, int port)
    {
        _ip = ip;
        _port = port;
    }

    public bool Connect()
    {
        try
        {
            if (_plc?.IsConnected ?? false)
            {
                return true;
            }

            _plc = new PlcClient(CpuType.S71200, _ip, 0, 1);
            _plc.Open();
            return _plc.IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connect failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ConnectAsync()
    {
        if (_plc?.IsConnected ?? false)
        {
            return true;
        }

        _plc?.Close();
        _plc = new PlcClient(CpuType.S71200, _ip, 0, 1);

        try
        {
            var connectTask = Task.Run(() => _plc.Open());
            if (await Task.WhenAny(connectTask, Task.Delay(3000)) != connectTask)
            {
                Console.WriteLine("Connect timeout");
                return false;
            }

            await connectTask;
            return _plc.IsConnected;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connect failed: {ex.Message}");
            return false;
        }
    }

    public void Disconnect() => _plc?.Close();

    public List<T> ReadData<T>(string address, ushort length)
        => ReadDataAsync<T>(address, length).GetAwaiter().GetResult();

    public async Task<List<T>> ReadDataAsync<T>(string address, ushort length)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("PLC is not connected.");
        }

        await _semaphore.WaitAsync();
        try
        {
            var result = new List<T>();
            for (var i = 0; i < length; i++)
            {
                var currentAddress = GetIndexedAddress(address, i);
                var value = await Task.Run(() => _plc.Read(currentAddress));
                if (value is null)
                {
                    throw new Exception($"Read {currentAddress} returned null.");
                }

                result.Add(ConvertValue<T>(value));
            }

            return result;
        }
        catch (Exception ex)
        {
            throw new Exception($"Read {address} failed: {ex.Message}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void WriteData<T>(string address, List<T> data)
        => WriteDataAsync(address, data).GetAwaiter().GetResult();

    public async Task WriteDataAsync<T>(string address, List<T> data)
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("PLC is not connected.");
        }

        await _semaphore.WaitAsync();
        try
        {
            for (var i = 0; i < data.Count; i++)
            {
                var currentAddress = GetIndexedAddress(address, i);
                await Task.Run(() => _plc.Write(currentAddress, (object)data[i]!));
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Write {address} failed: {ex.Message}", ex);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        try
        {
            Disconnect();
            _plc = null!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Dispose failed: {ex.Message}");
        }
    }

    private static string GetIndexedAddress(string baseAddress, int index)
        => baseAddress.Contains('[') ? baseAddress : $"{baseAddress}[{index}]";

    private static T ConvertValue<T>(object value)
    {
        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new Exception(
                $"Convert {value.GetType().Name} to {typeof(T).Name} failed.",
                ex);
        }
    }
}
