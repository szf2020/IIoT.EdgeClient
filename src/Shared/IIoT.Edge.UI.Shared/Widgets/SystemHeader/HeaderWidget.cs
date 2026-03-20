using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace IIoT.Edge.UI.Shared.Widgets.SystemHeader
{
    public class HeaderWidget : WidgetBase
    {
        public override string WidgetId => "Core.SystemHeader";
        public override string WidgetName => "系统头部导航";

        private string _systemTitle = "模切数采系统";
        public string SystemTitle { get => _systemTitle; set { _systemTitle = value; OnPropertyChanged(); } }

        public ICommand WindowControlCommand { get; }

        public HeaderWidget()
        {
            LayoutRow = 0;
            LayoutColumn = 0;
            ColumnSpan = 12;

            // 绑定窗口操作命令
            WindowControlCommand = new BaseCommand(ExecuteWindowControl);
        }

        private void ExecuteWindowControl(object? parameter)
        {
            if (parameter == null) return;

            // 【暴力获取窗体】：优先取当前激活的窗体，如果没有，强制取主窗体
            Window win = Application.Current.Windows.OfType<Window>().FirstOrDefault(x => x.IsActive)
                         ?? Application.Current.MainWindow;

            if (win == null) return;

            switch (parameter.ToString())
            {
                case "Min":
                    win.WindowState = WindowState.Minimized;
                    break;

                case "Max":
                    // 切换最大化/还原
                    win.WindowState = win.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                    break;

                case "Close":
                    // 彻底关闭应用程序
                    Application.Current.Shutdown();
                    break;
            }
        }
    }
}