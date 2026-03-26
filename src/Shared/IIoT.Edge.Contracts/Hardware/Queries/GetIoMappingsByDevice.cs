using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Contracts.Hardware.Queries;

public record IoMappingPagedDto(
    List<IoMappingEntity> Items,
    int TotalCount
);

public record GetIoMappingsByDeviceQuery(
    int NetworkDeviceId,
    int PageIndex,
    int PageSize
) : IQuery<Result<IoMappingPagedDto>>;