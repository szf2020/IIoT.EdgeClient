namespace IIoT.Edge.UI.Shared.PluginSystem;

public interface IEdgeWidget
{
    string WidgetId { get; }
    string WidgetName { get; }

    // 云端驱动布局元数据
    int LayoutRow { get; set; }

    int LayoutColumn { get; set; }
    int RowSpan { get; set; }
    int ColumnSpan { get; set; }
    bool IsVisible { get; set; }
}