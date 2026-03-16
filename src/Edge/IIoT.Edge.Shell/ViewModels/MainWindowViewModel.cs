using System.Collections.ObjectModel;
using IIoT.Edge.UI.Shared.WpfBase;
using IIoT.Edge.UI.Shared.Modularity;
using System.Windows;

namespace IIoT.Edge.Shell.ViewModels
{
    public class MainWindowViewModel : BaseNotifyPropertyChanged
    {
        public ObservableCollection<MenuInfo> Menus { get; } = new();
        public ObservableCollection<object> Documents { get; } = new();
        public ObservableCollection<object> Anchorables { get; } = new();

        // 补回这个属性，修复 MainWindow.xaml 或 NavigationService 的报错
        private object? _currentView;

        public object? CurrentView
        {
            get => _currentView;
            set { _currentView = value; OnPropertyChanged(); }
        }

        private MenuInfo? _selectedMenu;

        public MenuInfo? SelectedMenu
        {
            get => _selectedMenu;
            set { _selectedMenu = value; OnPropertyChanged(); }
        }
    }
}