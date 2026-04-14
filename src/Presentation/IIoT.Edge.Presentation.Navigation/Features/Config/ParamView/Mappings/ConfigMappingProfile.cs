using AutoMapper;
using IIoT.Edge.Domain.Config.Aggregates;
using IIoT.Edge.Application.Features.Config.ParamView.Models;

namespace IIoT.Edge.Presentation.Navigation.Features.Config.ParamView.Mappings;

/// <summary>
/// 参数页面映射配置。
/// 负责将配置领域实体映射为界面可编辑模型。
/// </summary>
public class ConfigMappingProfile : Profile
{
    public ConfigMappingProfile()
    {
        // SystemConfigEntity -> GeneralParamVm，仅保留界面展示所需的正向映射。
        CreateMap<SystemConfigEntity, GeneralParamVm>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Key))
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Value))
            .ForMember(d => d.Description,
                o => o.MapFrom(s => s.Description ?? ""));

        // 已移除 GeneralParamVm -> SystemConfigEntity 的反向映射，避免调用受保护构造函数。

        // DeviceParamEntity -> DeviceParamVm，仅保留界面展示所需的正向映射。
        CreateMap<DeviceParamEntity, DeviceParamVm>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Value))
            .ForMember(d => d.Unit, o => o.MapFrom(s => s.Unit ?? ""))
            .ForMember(d => d.Min, o => o.MapFrom(s => s.MinValue ?? ""))
            .ForMember(d => d.Max, o => o.MapFrom(s => s.MaxValue ?? ""));

        // 已移除 DeviceParamVm -> DeviceParamEntity 的反向映射，避免调用受保护构造函数。
    }
}
