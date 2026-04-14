using IIoT.Edge.Application.Abstractions.DataPipeline;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Runtime.Base;
using IIoT.Edge.SharedKernel.DataPipeline;

namespace IIoT.Edge.Runtime.DataPipeline.Tasks;

public class ProcessQueueTask : ScheduledTaskBase
{
    private readonly IDataPipelineService _pipelineService;
    private readonly List<ICellDataConsumer> _consumers;
    private readonly IFailedRecordStore _failedStore;

    public override string TaskName => "ProcessQueueTask";
    protected override int ExecuteInterval => 50;

    public ProcessQueueTask(
        ILogService logger,
        IDataPipelineService pipelineService,
        IEnumerable<ICellDataConsumer> consumers,
        IFailedRecordStore failedStore)
        : base(logger)
    {
        _pipelineService = pipelineService;
        _failedStore = failedStore;
        _consumers = consumers.OrderBy(c => c.Order).ToList();
    }

    protected override async Task ExecuteAsync()
    {
        if (!_pipelineService.TryDequeue(out var record) || record is null)
        {
            return;
        }

        var label = record.CellData.DisplayLabel;
        Logger.Info($"[{record.CellData.ProcessType}] Start processing {label}");

        foreach (var consumer in _consumers)
        {
            try
            {
                var success = await consumer.ProcessAsync(record).ConfigureAwait(false);
                if (!success)
                {
                    await HandleFailureAsync(record, consumer, "Consumer returned false.");
                }
            }
            catch (Exception ex)
            {
                await HandleFailureAsync(record, consumer, ex.Message);
            }
        }

        Logger.Info($"[{record.CellData.ProcessType}] {label} processing chain completed.");
    }

    private async Task HandleFailureAsync(
        CellCompletedRecord record,
        ICellDataConsumer consumer,
        string errorMessage)
    {
        var label = record.CellData.DisplayLabel;

        if (consumer.RetryChannel is null)
        {
            Logger.Warn($"[{record.CellData.ProcessType}] {consumer.Name} failed for {label}: {errorMessage} (no retry)");
            return;
        }

        Logger.Warn(
            $"[{record.CellData.ProcessType}] {consumer.Name} failed for {label}. Move to retry channel {consumer.RetryChannel}.");

        try
        {
            await _failedStore.SaveAsync(record, consumer.Name, errorMessage, consumer.RetryChannel);
        }
        catch (Exception ex)
        {
            Logger.Error($"[{record.CellData.ProcessType}] Save retry record failed for {label}: {ex.Message}");
        }
    }
}
