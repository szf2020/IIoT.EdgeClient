// 路径：src/Shared/IIoT.Edge.UI.Shared/Modularity/AnchorableInfo.cs
namespace IIoT.Edge.UI.Shared.Modularity
{
    public enum AnchorablePosition { Left, Right, Bottom, Main }

    public class AnchorableInfo
    {
        public string Title { get; set; } = string.Empty;
        public string ContentId { get; set; } = string.Empty;
        public AnchorablePosition InitialPosition { get; set; } = AnchorablePosition.Main;
        public bool IsVisible { get; set; } = true;
    }
}