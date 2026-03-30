using System;
using System.Collections.Generic;
using System.Text;

namespace IIoT.Edge.Contracts.DataPipeline;

// Contracts 层
public interface ICapacitySyncTask
{
    Task StartAsync(CancellationToken ct);
    Task StopAsync();
}
