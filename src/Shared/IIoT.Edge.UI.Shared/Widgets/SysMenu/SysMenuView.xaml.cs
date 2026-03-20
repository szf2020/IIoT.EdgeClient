using System.Windows;
using System.Windows.Controls;

namespace IIoT.Edge.UI.Shared.Widgets.SysMenu
{
    public partial class SysMenuView : UserControl
    {
        private SysMenuWidget? _vm;

        public SysMenuView()
        {
            InitializeComponent();
            this.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SysMenuWidget vm)
                _vm = vm;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string widgetId && _vm is not null)
            {
                if (_vm.NavigateCommand.CanExecute(widgetId))
                    _vm.NavigateCommand.Execute(widgetId);
            }
        }

        private void LoginBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_vm?.LoginCommand.CanExecute(null) == true)
                _vm.LoginCommand.Execute(null);
        }
    }
}