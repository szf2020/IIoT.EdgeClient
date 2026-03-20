using IIoT.Edge.UI.Shared.Widgets.SystemHeader;
using System.Windows;

namespace IIoT.Edge.Shell
{
    public partial class MainWindow : Window
    {
        // 对外暴露头部的 ViewModel
        public HeaderWidget HeaderViewModel { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            // 实例化 ViewModel
            HeaderViewModel = new HeaderWidget();

            // 绑定当前窗口的数据上下文
            this.DataContext = this;
        }
    }
}