// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/LogEntry.cs
namespace IIoT.Edge.Contracts.Model
{
    /// <summary>单条日志记录数据模型</summary>
    public class LogEntry
    {
        public DateTime Time { get; init; }
        public string Level { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
    }
}