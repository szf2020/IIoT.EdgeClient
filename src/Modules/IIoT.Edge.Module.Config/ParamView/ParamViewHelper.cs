using IIoT.Edge.Common.Enums;

namespace IIoT.Edge.Module.Config.ParamView;

public static class ParamViewHelper
{
    public static List<string> SystemConfigKeyNames { get; }
        = Enum.GetNames<SystemConfigKey>().ToList();

    public static List<string> DeviceParamKeyNames { get; }
        = Enum.GetNames<DeviceParamKey>().ToList();
}