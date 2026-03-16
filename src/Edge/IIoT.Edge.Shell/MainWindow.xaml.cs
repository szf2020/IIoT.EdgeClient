// 路径：src/Edge/IIoT.Edge.Shell/MainWindow.xaml.cs
using IIoT.Edge.Shell.ViewModels;
using IIoT.Edge.UI.Shared.Modularity;
using System.Windows;
using System.Windows.Controls;

namespace IIoT.Edge.Shell
{
    // 必须是 public
    public partial class MainWindow : Window
    {
        private readonly INavigationService _navigationService;

        // 通过 DI 容器自动注入 ViewModel 和 导航服务
        public MainWindow(MainWindowViewModel viewModel, INavigationService navigationService)
        {
            InitializeComponent();
            DataContext = viewModel;
            _navigationService = navigationService;
        }

        private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MenuListBox.SelectedItem is MenuInfo menu)
            {
                // 触发页面切换
                _navigationService.NavigateTo(menu.RouteName);
            }
        }
    }
}