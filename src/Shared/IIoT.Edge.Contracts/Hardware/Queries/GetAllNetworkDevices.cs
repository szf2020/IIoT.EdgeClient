using IIoT.Edge.Common.Messaging;
using IIoT.Edge.Common.Result;
using IIoT.Edge.Domain.Hardware.Aggregates;

namespace IIoT.Edge.Contracts.Hardware.Queries;

public record GetAllNetworkDevicesQuery() : IQuery<Result<List<NetworkDeviceEntity>>>;