// 路径：src/Edge/IIoT.Edge.Shell/ViewModels/MainWindowViewModel.cs
using IIoT.Edge.UI.Shared.Modularity;
using IIoT.Edge.UI.Shared.WpfBase; // 引用你老项目优秀的基类
using System.Collections.ObjectModel;

namespace IIoT.Edge.Shell.ViewModels
{
    // 必须是 public
    public class MainWindowViewModel : BaseNotifyPropertyChanged
    {
        private object _currentView;

        // 绑定到主界面中心区域的当前页面
        public object CurrentView
        {
            get => _currentView;
            set
            {
                _currentView = value;
                OnPropertyChanged(); // 触发老项目基类里的通知方法
            }
        }

        // 绑定到左侧菜单的数据源
        public ObservableCollection<MenuInfo> Menus { get; set; } = new ObservableCollection<MenuInfo>();
    }
}