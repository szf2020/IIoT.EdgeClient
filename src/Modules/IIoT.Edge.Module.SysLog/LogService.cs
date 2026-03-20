// 路径：src/Modules/IIoT.Edge.Module.SysLog/LogService.cs
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Model;
using log4net;
using log4net.Config;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace IIoT.Edge.Module.SysLog
{
    /// <summary>
    /// ILogService 实现。
    /// 替代老项目的静态 VisionLog，支持 DI 注入，线程安全写入 UI 集合。
    /// </summary>
    public class LogService : ILogService
    {
        private static readonly ILog _log4net =
            LogManager.GetLogger("logLogger");

        public ObservableCollection<LogEntry> Entries { get; } = new();

        public event Action<LogEntry>? EntryAdded;

        public LogService()
        {
            // 加载 log4net 配置文件
            var configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "log4net.config");

            if (File.Exists(configPath))
                XmlConfigurator.Configure(new FileInfo(configPath));
        }

        public void Debug(string message) => Write("DEBUG", message,
            () => _log4net.Debug(message));

        public void Info(string message) => Write("INFO", message,
            () => _log4net.Info(message));

        public void Warn(string message) => Write("WARN", message,
            () => _log4net.Warn(message));

        public void Error(string message) => Write("ERROR", message,
            () => _log4net.Error(message));

        public void Fatal(string message) => Write("FATAL", message,
            () => _log4net.Fatal(message));

        private void Write(string level, string message, Action log4netWrite)
        {
            // 写文件（任意线程）
            log4netWrite();

            var entry = new LogEntry
            {
                Time = DateTime.Now,
                Level = level,
                Message = message
            };

            // 写 UI 集合必须在 UI 线程
            Application.Current.Dispatcher.Invoke(() =>
            {
                Entries.Insert(0, entry);
                if (Entries.Count > 200)
                    Entries.RemoveAt(Entries.Count - 1);
            }, DispatcherPriority.Background);

            EntryAdded?.Invoke(entry);
        }
    }
}