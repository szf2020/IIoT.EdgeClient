using IIoT.Edge.UI.Shared.Mvvm;

namespace IIoT.Edge.UI.Shared.PluginSystem
{
    /// <summary>
    /// 所有动态 UI 插件的基础类
    /// </summary>
    public abstract class WidgetBase : BaseControlNotifyPropertyChanged, IEdgeWidget
    {
        public abstract string WidgetId { get; }
        public abstract string WidgetName { get; }

        // 布局属性：通过基类的 OnPropertyChanged 触发 WPF 界面刷新
        private int _row;

        public int LayoutRow { get => _row; set { _row = value; OnPropertyChanged(); } }

        private int _col;
        public int LayoutColumn { get => _col; set { _col = value; OnPropertyChanged(); } }

        private int _rowSpan = 1;
        public int RowSpan { get => _rowSpan; set { _rowSpan = value; OnPropertyChanged(); } }

        private int _colSpan = 1;
        public int ColumnSpan { get => _colSpan; set { _colSpan = value; OnPropertyChanged(); } }

        private bool _isVisible = true;
        public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }
    }
}