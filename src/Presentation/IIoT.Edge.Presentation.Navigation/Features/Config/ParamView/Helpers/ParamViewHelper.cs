using IIoT.Edge.SharedKernel.Enums;

namespace IIoT.Edge.Presentation.Navigation.Features.Config.ParamView;

/// <summary>
/// 参数页面辅助类。
/// 提供系统配置键与设备参数键的枚举名称集合。
/// </summary>
public static class ParamViewHelper
{
    public static List<string> SystemConfigKeyNames { get; }
        = Enum.GetNames<SystemConfigKey>().ToList();

    public static List<string> DeviceParamKeyNames { get; }
        = Enum.GetNames<DeviceParamKey>().ToList();
}
