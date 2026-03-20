using System.Windows.Controls;
using System.Windows.Input;

namespace IIoT.Edge.UI.Shared.Widgets.SystemHeader
{
    public partial class HeaderView : UserControl
    {
        public HeaderView()
        {
            InitializeComponent();
            // ❌ 绝对不要 this.DataContext = new HeaderWidget()
            // ✅ DataContext 由 MainWindow 的 DataTemplate 自动注入
        }

        /// <summary>
        /// 鼠标拖拽：View 层合法的 UI 调度，转发给 ViewModel Command
        /// </summary>
        private void Border_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is HeaderWidget vm)
            {
                if (vm.WindowDragCommand.CanExecute(null))
                    vm.WindowDragCommand.Execute(null);
            }
        }
    }
}