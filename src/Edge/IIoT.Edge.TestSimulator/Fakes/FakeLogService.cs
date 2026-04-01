using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Model;
using System.Collections.ObjectModel;
using System.Windows;

namespace IIoT.Edge.TestSimulator.Fakes;

/// <summary>
/// 替换真实日志服务，同时把日志转发到 ViewModel 界面显示
/// </summary>
public sealed class FakeLogService : ILogService
{
    public ObservableCollection<LogEntry> Entries { get; } = new();

    public event Action<LogEntry>? EntryAdded;

    private void Add(string level, string message)
    {
        var entry = new LogEntry
        {
            Time    = DateTime.Now,
            Level   = level,
            Message = message
        };

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
            dispatcher.Invoke(() => Entries.Add(entry));
        else
            Entries.Add(entry);

        EntryAdded?.Invoke(entry);
    }

    public void Debug(string message) => Add("DEBUG", message);
    public void Info(string message)  => Add("INFO",  message);
    public void Warn(string message)  => Add("WARN",  message);
    public void Error(string message) => Add("ERROR", message);
    public void Fatal(string message) => Add("FATAL", message);
}
