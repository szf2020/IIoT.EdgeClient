// 路径：src/Shared/IIoT.Edge.UI.Shared/Widgets/SystemHeader/HeaderWidget.cs
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace IIoT.Edge.UI.Shared.Widgets.SystemHeader
{
    /// <summary>
    /// 系统头部导航 ViewModel。
    /// 纯净的 MVVM — 绝对不 new View，不 new Window，所有窗口操作通过 Command 下发。
    /// </summary>
    public class HeaderWidget : WidgetBase
    {
        // ── IEdgeWidget 契约 ─────────────────────────────────────────
        public override string WidgetId   => "Core.SystemHeader";
        public override string WidgetName => "系统头部导航";

        // ── 标题 ──────────────────────────────────────────────────────
        private string _systemTitle = "Iiot客户端系统";
        public string SystemTitle
        {
            get => _systemTitle;
            set { _systemTitle = value; OnPropertyChanged(); }
        }

        // ── 登录用户信息（后期可由 AuthService 注入） ─────────────────
        private string _currentUser = "Admin";
        public string CurrentUser
        {
            get => _currentUser;
            set { _currentUser = value; OnPropertyChanged(); }
        }

        // ── 最大化图标切换（绑定 PackIcon.Kind 用字符串） ────────────
        private string _maxRestoreIcon = "WindowMaximize";
        public string MaxRestoreIcon
        {
            get => _maxRestoreIcon;
            set { _maxRestoreIcon = value; OnPropertyChanged(); }
        }

        // ── Commands ─────────────────────────────────────────────────
        public ICommand WindowControlCommand { get; }
        public ICommand WindowDragCommand    { get; }

        public HeaderWidget()
        {
            // 布局占位：第 0 行，跨满 12 列（与 MainWindow Grid 对齐）
            LayoutRow    = 0;
            LayoutColumn = 0;
            ColumnSpan   = 12;

            WindowControlCommand = new BaseCommand(ExecuteWindowControl);
            WindowDragCommand    = new BaseCommand(ExecuteWindowDrag);
        }

        // ── 窗口控制逻辑 ─────────────────────────────────────────────
        private void ExecuteWindowControl(object? parameter)
        {
            if (parameter is null) return;

            // 获取当前激活窗体；无激活时降级为 MainWindow
            Window? win = Application.Current.Windows
                              .OfType<Window>()
                              .FirstOrDefault(w => w.IsActive)
                          ?? Application.Current.MainWindow;

            if (win is null) return;

            switch (parameter.ToString())
            {
                case "Min":
                    win.WindowState = WindowState.Minimized;
                    break;

                case "Max":
                    win.WindowState = win.WindowState == WindowState.Maximized
                        ? WindowState.Normal
                        : WindowState.Maximized;
                    // 同步切换最大化图标
                    MaxRestoreIcon = win.WindowState == WindowState.Maximized
                        ? "WindowRestore"
                        : "WindowMaximize";
                    break;

                case "Close":
                    Application.Current.Shutdown();
                    break;
            }
        }

        // ── 窗口拖拽逻辑（由 View 的 MouseLeftButtonDown 触发） ───────
        private void ExecuteWindowDrag(object? parameter)
        {
            Window? win = Application.Current.Windows
                              .OfType<Window>()
                              .FirstOrDefault(w => w.IsActive)
                          ?? Application.Current.MainWindow;

            // 最大化状态下拖拽先还原，再跟随鼠标
            if (win?.WindowState == WindowState.Maximized)
            {
                win.WindowState = WindowState.Normal;
                MaxRestoreIcon  = "WindowMaximize";
            }

            win?.DragMove();
        }
    }
}
