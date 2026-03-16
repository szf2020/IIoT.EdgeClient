using IIoT.Edge.SharedKernel.WpfBase;

namespace IIoT.Edge.UI.Shared.PluginSystem;

// 注意：继承你上传代码里的 BaseControlNotifyPropertyChanged
public abstract class WidgetBase : BaseControlNotifyPropertyChanged, IEdgeWidget
{
    public abstract string WidgetId { get; }
    public abstract string WidgetName { get; }

    // 布局属性：利用基类的 OnPropertyChanged 实现 UI 响应
    private int _row; public int LayoutRow { get => _row; set { _row = value; OnPropertyChanged(); } }

    private int _col; public int LayoutColumn { get => _col; set { _col = value; OnPropertyChanged(); } }
    private int _rowSpan = 1; public int RowSpan { get => _rowSpan; set { _rowSpan = value; OnPropertyChanged(); } }
    private int _colSpan = 1; public int ColumnSpan { get => _colSpan; set { _colSpan = value; OnPropertyChanged(); } }
    private bool _isVisible = true; public bool IsVisible { get => _isVisible; set { _isVisible = value; OnPropertyChanged(); } }
}