using AutoMapper;
using IIoT.Edge.Domain.Config.Aggregates;
using IIoT.Edge.Module.Config.ParamView.Models;

namespace IIoT.Edge.Module.Config.ParamView.Mappings;

public class ConfigMappingProfile : Profile
{
    public ConfigMappingProfile()
    {
        // ── SystemConfig ↔ GeneralParamVm ──

        CreateMap<SystemConfigEntity, GeneralParamVm>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Key))
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Value))
            .ForMember(d => d.Description,
                o => o.MapFrom(s => s.Description ?? ""));

        CreateMap<GeneralParamVm, SystemConfigEntity>()
            .DisableCtorValidation()
            .ConstructUsing(s => new SystemConfigEntity())
            .ForMember(d => d.Key, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Value))
            .ForMember(d => d.Description,
                o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Id, o => o.Ignore());

        // ── DeviceParam ↔ DeviceParamVm ──

        CreateMap<DeviceParamEntity, DeviceParamVm>()
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Value))
            .ForMember(d => d.Unit, o => o.MapFrom(s => s.Unit ?? ""))
            .ForMember(d => d.Min, o => o.MapFrom(s => s.MinValue ?? ""))
            .ForMember(d => d.Max, o => o.MapFrom(s => s.MaxValue ?? ""));

        CreateMap<DeviceParamVm, DeviceParamEntity>()
            .DisableCtorValidation()
            .ConstructUsing(s => new DeviceParamEntity())
            .ForMember(d => d.Name, o => o.MapFrom(s => s.Name))
            .ForMember(d => d.Value, o => o.MapFrom(s => s.Value))
            .ForMember(d => d.Unit, o => o.MapFrom(s => s.Unit))
            .ForMember(d => d.MinValue, o => o.MapFrom(s => s.Min))
            .ForMember(d => d.MaxValue, o => o.MapFrom(s => s.Max))
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.NetworkDeviceId, o => o.Ignore())
            .ForMember(d => d.NetworkDevice, o => o.Ignore());
    }
}