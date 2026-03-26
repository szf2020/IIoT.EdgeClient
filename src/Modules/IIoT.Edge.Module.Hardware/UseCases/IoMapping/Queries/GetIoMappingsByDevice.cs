using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Contracts.Hardware.Queries;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Module.Hardware.UseCases.IoMapping.Queries;


public class GetIoMappingsByDeviceHandler(
    IReadRepository<IoMappingEntity> repo
) : IQueryHandler<GetIoMappingsByDeviceQuery, Result<IoMappingPagedDto>>
{
    public async Task<Result<IoMappingPagedDto>> Handle(
        GetIoMappingsByDeviceQuery request,
        CancellationToken cancellationToken)
    {
        var all = await repo.GetListAsync(
            x => x.NetworkDeviceId == request.NetworkDeviceId,
            cancellationToken);

        var totalCount = all.Count;
        var items = all
            .OrderBy(x => x.SortOrder)
            .Skip(request.PageIndex * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return Result.Success(new IoMappingPagedDto(items, totalCount));
    }
}