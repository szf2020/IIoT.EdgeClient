using AutoMapper;
using IIoT.Edge.Common.DataPipeline.CellData;

namespace IIoT.Edge.DataMapping.Cloud.Injection;

/// <summary>
/// 注液机 → 云端 DTO 的 AutoMapper Profile
/// 
/// InjectionCellData → InjectionCloudDto
/// bool? CellResult → string "OK"/"NG"
/// DeviceId 由 CloudConsumer 手动赋值（来自 DeviceService）
/// </summary>
public class InjectionCloudProfile : Profile
{
    public InjectionCloudProfile()
    {
        CreateMap<InjectionCellData, InjectionCloudDto>()
            .ForMember(d => d.CellResult,
                o => o.MapFrom(s => s.CellResult == true ? "OK" : "NG"))
            .ForMember(d => d.DeviceId, o => o.Ignore());
    }
}