// 路径：src/Modules/IIoT.Edge.Module.SysLog/LogWidget.cs
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;
using System.Windows.Input;
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Model;

namespace IIoT.Edge.Module.SysLog
{
    /// <summary>
    /// 日志停靠面板 ViewModel。
    /// 注入 ILogService，暴露 Entries 供 View 绑定。
    /// </summary>
    public class LogWidget : WidgetBase
    {
        public override string WidgetId => "Core.SysLog";
        public override string WidgetName => "系统日志";

        private readonly ILogService _logService;

        public ObservableCollection<LogEntry> Entries
            => _logService.Entries;

        public ICommand ClearCommand { get; }

        public LogWidget(ILogService logService)
        {
            _logService = logService;

            // 停靠面板布局占位
            LayoutRow = 1;
            LayoutColumn = 1;

            ClearCommand = new BaseCommand(_ =>
            {
                _logService.Entries.Clear();
            });
        }
    }
}