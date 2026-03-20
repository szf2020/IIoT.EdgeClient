namespace IIoT.Edge.UI.Shared.PluginSystem
{
    /// <summary>
    /// 边缘端 UI 插件通用契约
    /// </summary>
    public interface IEdgeWidget
    {
        string WidgetId { get; }      // 插件唯一标识
        string WidgetName { get; }    // 插件显示名称

        // 云端驱动的布局元数据
        int LayoutRow { get; set; }

        int LayoutColumn { get; set; }
        int RowSpan { get; set; }
        int ColumnSpan { get; set; }
        bool IsVisible { get; set; }
    }
}