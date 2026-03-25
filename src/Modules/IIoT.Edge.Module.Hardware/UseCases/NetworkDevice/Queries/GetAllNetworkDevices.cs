using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Module.Hardware.UseCases.NetworkDevice.Queries;

public record GetAllNetworkDevicesQuery() : IQuery<Result<List<NetworkDeviceEntity>>>;

public class GetAllNetworkDevicesHandler(
    IReadRepository<NetworkDeviceEntity> repo
) : IQueryHandler<GetAllNetworkDevicesQuery, Result<List<NetworkDeviceEntity>>>
{
    public async Task<Result<List<NetworkDeviceEntity>>> Handle(
        GetAllNetworkDevicesQuery request,
        CancellationToken cancellationToken)
    {
        var list = await repo.GetListAsync(_ => true, cancellationToken);
        return Result.Success(list);
    }
}