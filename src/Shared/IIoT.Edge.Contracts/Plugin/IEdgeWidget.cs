namespace IIoT.Edge.Contracts.Plugin
{
    /// <summary>
    /// 边缘端 UI 插件通用契约
    /// </summary>
    public interface IEdgeWidget
    {
        string WidgetId { get; }
        string WidgetName { get; }

        int LayoutRow { get; set; }
        int LayoutColumn { get; set; }
        int RowSpan { get; set; }
        int ColumnSpan { get; set; }
        bool IsVisible { get; set; }

        /// <summary>
        /// 页面被导航到时调用（每次切换页面都会触发）
        /// Widget 在此方法中重新加载最新数据
        /// </summary>
        Task OnActivatedAsync();
    }
}