using IIoT.Edge.Contracts.Plugin;
using IIoT.Edge.UI.Shared.Mvvm;

namespace IIoT.Edge.UI.Shared.PluginSystem
{
    public abstract class WidgetBase : BaseControlNotifyPropertyChanged, IEdgeWidget
    {
        public abstract string WidgetId { get; }
        public abstract string WidgetName { get; }

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

        /// <summary>
        /// 页面激活时调用，子类覆盖以重新加载数据
        /// 默认空实现，不强制所有 Widget 都实现
        /// </summary>
        public virtual Task OnActivatedAsync() => Task.CompletedTask;
    }
}