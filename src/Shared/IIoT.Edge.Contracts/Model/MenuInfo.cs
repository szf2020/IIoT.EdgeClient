// 路径：src/Shared/IIoT.Edge.UI.Shared/Model/MenuInfo.cs
namespace IIoT.Edge.Contracts.Model
{
    public class MenuInfo
    {
        /// <summary>菜单显示名</summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>导航目标 WidgetId，对应 IEdgeWidget.WidgetId</summary>
        public string WidgetId { get; set; } = string.Empty;

        /// <summary>MaterialDesign PackIconKind 名称</summary>
        public string Icon { get; set; } = string.Empty;

        /// <summary>排序序号</summary>
        public int Order { get; set; }

        /// <summary>
        /// 所需权限字符串，对应 Permissions 常量。
        /// 为空表示所有人可见，不需要登录。
        /// </summary>
        public string RequiredPermission { get; set; } = string.Empty;
    }
}