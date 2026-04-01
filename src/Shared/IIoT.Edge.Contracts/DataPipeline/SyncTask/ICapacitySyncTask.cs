using System;
using System.Collections.Generic;
using System.Text;

namespace IIoT.Edge.Contracts.DataPipeline.SyncTask;

/// <summary>
/// 产能定时同步任务
/// 60 秒间隔，在线时 POST 产能快照，失败写 SQLite
/// </summary>
public interface ICapacitySyncTask : ISyncTask { }
