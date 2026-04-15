using IIoT.Edge.Application.Abstractions.Tasks;

namespace IIoT.Edge.Application.Common.Tasks;

public sealed class LongRunningBackgroundTaskService : IManagedBackgroundService
{
    private readonly IBackgroundTask _task;
    private CancellationTokenSource? _linkedCts;
    private Task? _executionTask;

    public LongRunningBackgroundTaskService(IBackgroundTask task)
    {
        _task = task;
    }

    public string ServiceName => _task.TaskName;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_executionTask is not null)
        {
            return Task.CompletedTask;
        }

        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executionTask = Task.Run(() => _task.StartAsync(_linkedCts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executionTask is null || _linkedCts is null)
        {
            return;
        }

        await _linkedCts.CancelAsync();

        try
        {
            await _executionTask.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _linkedCts.Dispose();
            _linkedCts = null;
            _executionTask = null;
        }
    }
}
