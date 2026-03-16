using System.Windows;
using System.Windows.Controls;

namespace IIoT.Edge.UI.Shared.Layouts
{
    public partial class HeaderView : UserControl
    {
        private Window? mainWindow;

        public HeaderView()
        {
            InitializeComponent();
            this.Loaded += HeaderView_Loaded;
        }

        private void HeaderView_Loaded(object sender, RoutedEventArgs e)
        {
            // 为对应头部操作页面按钮绑定点击事件
            btn_MaxSize.Click += Btn_MaxSize_Click;
            btn_MiniSize.Click += Btn_MiniSize_Click;
            // 获取当前操作的主窗口实例
            mainWindow = Application.Current.MainWindow;
        }

        private void Btn_MiniSize_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null) mainWindow.WindowState = WindowState.Minimized;
        }

        private void Btn_MaxSize_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow == null) return;
            if (mainWindow.WindowState == WindowState.Maximized)
                mainWindow.WindowState = WindowState.Normal;
            else
                mainWindow.WindowState = WindowState.Maximized;
        }

        private void btn_Close_Click(object sender, RoutedEventArgs e) => Application.Current.Shutdown();
    }
}