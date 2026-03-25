using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Repository;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Module.Hardware.UseCases.IoMapping.Commands;

public record IoMappingDto(
    int Id,
    int NetworkDeviceId,
    string Label,
    string PlcAddress,
    int AddressCount,
    string DataType,
    string Direction,
    int SortOrder
);

public record SaveIoMappingsCommand(
    int NetworkDeviceId,
    List<IoMappingDto> Mappings
) : ICommand<Result>;

public class SaveIoMappingsHandler(
    IRepository<IoMappingEntity> repo
) : ICommandHandler<SaveIoMappingsCommand, Result>
{
    public async Task<Result> Handle(
        SaveIoMappingsCommand request,
        CancellationToken cancellationToken)
    {
        foreach (var dto in request.Mappings)
        {
            if (dto.Id == 0)
            {
                var entity = new IoMappingEntity(
                    request.NetworkDeviceId, dto.Label, dto.PlcAddress,
                    dto.AddressCount, dto.DataType, dto.Direction)
                {
                    SortOrder = dto.SortOrder
                };
                repo.Add(entity);
            }
            else
            {
                var entity = await repo.GetByIdAsync(dto.Id, cancellationToken);
                if (entity != null)
                {
                    entity.Label = dto.Label;
                    entity.PlcAddress = dto.PlcAddress;
                    entity.AddressCount = dto.AddressCount;
                    entity.DataType = dto.DataType;
                    entity.Direction = dto.Direction;
                    entity.SortOrder = dto.SortOrder;
                    repo.Update(entity);
                }
            }
        }

        await repo.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}