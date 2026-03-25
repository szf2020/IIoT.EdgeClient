using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Module.Hardware.UseCases.SerialDevice.Queries;

public record GetAllSerialDevicesQuery() : IQuery<Result<List<SerialDeviceEntity>>>;

public class GetAllSerialDevicesHandler(
    IReadRepository<SerialDeviceEntity> repo
) : IQueryHandler<GetAllSerialDevicesQuery, Result<List<SerialDeviceEntity>>>
{
    public async Task<Result<List<SerialDeviceEntity>>> Handle(
        GetAllSerialDevicesQuery request,
        CancellationToken cancellationToken)
    {
        var list = await repo.GetListAsync(_ => true, cancellationToken);
        return Result.Success(list);
    }
}