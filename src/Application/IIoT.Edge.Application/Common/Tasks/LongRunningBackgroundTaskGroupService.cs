using IIoT.Edge.Application.Abstractions.Tasks;

namespace IIoT.Edge.Application.Common.Tasks;

public sealed class LongRunningBackgroundTaskGroupService : IManagedBackgroundService
{
    private readonly IReadOnlyList<LongRunningBackgroundTaskService> _services;

    public LongRunningBackgroundTaskGroupService(
        string serviceName,
        IEnumerable<IBackgroundTask> tasks)
    {
        ServiceName = serviceName;
        _services = tasks
            .Select(task => new LongRunningBackgroundTaskService(task))
            .ToList()
            .AsReadOnly();
    }

    public string ServiceName { get; }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var service in _services)
        {
            _ = service.StartAsync(cancellationToken);
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        for (var index = _services.Count - 1; index >= 0; index--)
        {
            await _services[index].StopAsync(cancellationToken);
        }
    }
}
