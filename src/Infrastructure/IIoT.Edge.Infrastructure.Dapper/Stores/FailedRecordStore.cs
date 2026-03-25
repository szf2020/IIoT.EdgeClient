using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Infrastructure.Dapper.Connection;
using IIoT.Edge.Infrastructure.Dapper.Repository;
using System.Data;

namespace IIoT.Edge.Infrastructure.Dapper.Stores;

/// <summary>
/// 失败记录的 SQLite 存储实现
/// 
/// 对应数据库：pipeline.db
/// 对应表：failed_cell_records
/// 
/// ProcessQueueTask 消费失败时写入
/// RetryTask 定时捞出重试，成功则删除
/// </summary>
public class FailedRecordStore : DapperRepositoryBase<FailedCellRecord>, IFailedRecordStore
{
    public override string DbName => "pipeline";
    protected override string TableName => "failed_cell_records";

    protected override string CreateTableSql => @"
        CREATE TABLE IF NOT EXISTS failed_cell_records (
            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
            Barcode         TEXT    NOT NULL,
            LocalDeviceId   INTEGER NOT NULL,
            DeviceName      TEXT    NOT NULL,
            CloudDeviceCode TEXT,
            CellResult      INTEGER NOT NULL,
            DataJson        TEXT    NOT NULL,
            CompletedTime   TEXT    NOT NULL,
            FailedTarget    TEXT    NOT NULL,
            ErrorMessage    TEXT    NOT NULL DEFAULT '',
            RetryCount      INTEGER NOT NULL DEFAULT 0,
            NextRetryTime   TEXT    NOT NULL,
            CreatedAt       TEXT    NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_failed_cell_retry 
            ON failed_cell_records (NextRetryTime, RetryCount);

        CREATE INDEX IF NOT EXISTS idx_failed_cell_barcode 
            ON failed_cell_records (Barcode);
    ";

    public FailedRecordStore(
        SqliteConnectionFactory connectionFactory,
        ILogService logger)
        : base(connectionFactory, logger)
    {
    }

    /// <summary>
    /// 存入一条失败记录
    /// </summary>
    public async Task SaveAsync(
        CellCompletedRecord record,
        string failedTarget,
        string errorMessage)
    {
        const string sql = @"
            INSERT INTO failed_cell_records 
                (Barcode, LocalDeviceId, DeviceName, CloudDeviceCode,
                 CellResult, DataJson, CompletedTime,
                 FailedTarget, ErrorMessage, RetryCount, NextRetryTime, CreatedAt)
            VALUES 
                (@Barcode, @LocalDeviceId, @DeviceName, @CloudDeviceCode,
                 @CellResult, @DataJson, @CompletedTime,
                 @FailedTarget, @ErrorMessage, 0, @NextRetryTime, @CreatedAt)";

        var now = DateTime.Now;

        await SafeExecuteAsync(sql, new
        {
            record.Barcode,
            record.LocalDeviceId,
            record.DeviceName,
            record.CloudDeviceCode,
            record.CellResult,
            record.DataJson,
            CompletedTime = record.CompletedTime.ToString("O"),
            FailedTarget = failedTarget,
            ErrorMessage = errorMessage,
            NextRetryTime = now.AddSeconds(30).ToString("O"),
            CreatedAt = now.ToString("O")
        });

        Logger.Info($"[重传队列] 条码: {record.Barcode} 已存入" +
            $"（失败环节: {failedTarget}）");
    }

    /// <summary>
    /// 获取待重试的记录（NextRetryTime 已到期、且未超过最大重试次数）
    /// </summary>
    public async Task<List<FailedCellRecord>> GetPendingAsync(int batchSize = 10)
    {
        const string sql = @"
            SELECT * FROM failed_cell_records 
            WHERE NextRetryTime <= @Now
              AND RetryCount <= 20
            ORDER BY CreatedAt ASC
            LIMIT @BatchSize";

        var result = await SafeQueryAsync(sql, new
        {
            Now = DateTime.Now.ToString("O"),
            BatchSize = batchSize
        });

        return result.ToList();
    }

    /// <summary>
    /// 重试成功，删除记录
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        await DeleteByIdAsync(id);
    }

    /// <summary>
    /// 重试失败，更新重试信息
    /// </summary>
    public async Task UpdateRetryAsync(
        long id,
        int retryCount,
        string errorMessage,
        DateTime nextRetryTime)
    {
        const string sql = @"
            UPDATE failed_cell_records 
            SET RetryCount = @RetryCount,
                ErrorMessage = @ErrorMessage,
                NextRetryTime = @NextRetryTime
            WHERE Id = @Id";

        await SafeExecuteAsync(sql, new
        {
            Id = id,
            RetryCount = retryCount,
            ErrorMessage = errorMessage,
            NextRetryTime = nextRetryTime.ToString("O")
        });
    }

    /// <summary>
    /// 获取当前失败记录总数（UI 监控用）
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        return await SafeCountAsync(
            $"SELECT COUNT(*) FROM {TableName}");
    }

    /// <summary>
    /// 重置所有 Abandoned 记录为可重试状态（UI "全部重传" 按钮调用）
    /// </summary>
    public async Task ResetAllAbandonedAsync()
    {
        const string sql = @"
            UPDATE failed_cell_records 
            SET RetryCount = 0,
                NextRetryTime = @Now
            WHERE RetryCount > 20";

        var affected = await SafeExecuteAsync(sql, new
        {
            Now = DateTime.Now.ToString("O")
        });

        if (affected > 0)
            Logger.Info($"[重传队列] 已重置 {affected} 条 Abandoned 记录");
    }
}