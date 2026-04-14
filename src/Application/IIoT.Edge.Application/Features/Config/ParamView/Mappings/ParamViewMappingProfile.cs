using AutoMapper;
using IIoT.Edge.Application.Features.Config.ParamView.Models;
using IIoT.Edge.Application.Features.Config.UseCases.DeviceParam.Commands;
using IIoT.Edge.Application.Features.Config.UseCases.SystemConfig.Commands;

namespace IIoT.Edge.Application.Features.Config.ParamView.Mappings;

public class ParamViewMappingProfile : Profile
{
    public ParamViewMappingProfile()
    {
        CreateMap<GeneralParamVm, SystemConfigDto>()
            .ConstructUsing(src => new SystemConfigDto(src.Name, src.Value, src.Description));

        CreateMap<DeviceParamVm, DeviceParamDto>()
            .ConstructUsing(src => new DeviceParamDto(src.Name, src.Value, src.Unit, src.Min, src.Max));
    }
}
