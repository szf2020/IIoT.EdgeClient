using System;
using System.Collections.Generic;
using System.Text;

namespace IIoT.Edge.Contracts.DataPipeline.SyncTask;

/// <summary>
/// 设备日志定时同步任务
/// 60 秒间隔，内存队列 → 批量 POST 云端，失败写 SQLite
/// </summary>
public interface IDeviceLogSyncTask : ISyncTask { }