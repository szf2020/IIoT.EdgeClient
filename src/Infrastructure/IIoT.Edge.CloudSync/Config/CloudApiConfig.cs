// 路径：src/Infrastructure/IIoT.Edge.CloudSync/Config/CloudApiConfig.cs
namespace IIoT.Edge.CloudSync.Config
{
    public class CloudApiConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutSecs { get; set; } = 10;
    }
}