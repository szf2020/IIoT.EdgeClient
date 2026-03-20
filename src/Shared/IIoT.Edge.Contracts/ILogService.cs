// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/ILogService.cs
using IIoT.Edge.Contracts.Model;
using System.Collections.ObjectModel;

namespace IIoT.Edge.Contracts
{
    /// <summary>
    /// 日志服务契约。
    /// 接口定义在 UI.Shared，实现在 IIoT.Edge.Module.SysLog。
    /// 替代老项目的静态 VisionLog 类。
    /// </summary>
    public interface ILogService
    {
        /// <summary>供 UI 绑定的日志条目集合，线程安全写入</summary>
        ObservableCollection<LogEntry> Entries { get; }

        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Fatal(string message);

        /// <summary>每写入一条日志触发一次，参数为新条目</summary>
        event Action<LogEntry> EntryAdded;
    }
}