using System.Windows.Controls;

namespace IIoT.Edge.UI.Shared.Widgets.SystemHeader
{
    public partial class HeaderView : UserControl
    {
        public HeaderView()
        {
            InitializeComponent();

            // 【极其重要】：为了让你在独立测试这个 UserControl 时按钮能点，
            // 必须临时把 DataContext 指向你的 ViewModel。
            // (等之后接入主程序 ItemsControl 动态生成时，可以把这行删掉，由主程序自动注入)
            this.DataContext = new HeaderWidget();
        }
    }
}