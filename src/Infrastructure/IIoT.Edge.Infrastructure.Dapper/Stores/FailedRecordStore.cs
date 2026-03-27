using Dapper;
using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Infrastructure.Dapper.Connection;
using IIoT.Edge.Infrastructure.Dapper.Repository;

namespace IIoT.Edge.Infrastructure.Dapper.Stores;

/// <summary>
/// 失败记录的 SQLite 存储实现
/// 
/// 对应数据库：pipeline.db
/// 对应表：failed_cell_records
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
            CloudDeviceId   TEXT,
            CellResult      INTEGER NOT NULL,
            DataJson        TEXT    NOT NULL,
            CompletedTime   TEXT    NOT NULL,
            FailedTarget    TEXT    NOT NULL,
            ErrorMessage    TEXT    NOT NULL,
            RetryCount      INTEGER NOT NULL DEFAULT 0,
            NextRetryTime   TEXT    NOT NULL,
            CreatedAt       TEXT    NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_failed_next_retry
            ON failed_cell_records (NextRetryTime);

        CREATE INDEX IF NOT EXISTS idx_failed_barcode
            ON failed_cell_records (Barcode);
    ";

    public FailedRecordStore(
        SqliteConnectionFactory connectionFactory,
        ILogService logger)
        : base(connectionFactory, logger)
    {
    }

    /// <summary>
    /// 保存失败记录
    /// </summary>
    public async Task SaveAsync(
        CellCompletedRecord record,
        string failedTarget,
        string errorMessage)
    {
        const string sql = @"
            INSERT INTO failed_cell_records
                (Barcode, LocalDeviceId, DeviceName, CloudDeviceId,
                 CellResult, DataJson, CompletedTime,
                 FailedTarget, ErrorMessage, RetryCount, NextRetryTime, CreatedAt)
            VALUES
                (@Barcode, @LocalDeviceId, @DeviceName, @CloudDeviceId,
                 @CellResult, @DataJson, @CompletedTime,
                 @FailedTarget, @ErrorMessage, 0, @NextRetryTime, @CreatedAt)";

        await SafeExecuteAsync(sql, new
        {
            record.Barcode,
            record.LocalDeviceId,
            record.DeviceName,
            CloudDeviceId = record.CloudDeviceId?.ToString(),
            record.CellResult,
            record.DataJson,
            CompletedTime = record.CompletedTime.ToString("O"),
            FailedTarget = failedTarget,
            ErrorMessage = errorMessage,
            NextRetryTime = DateTime.Now.AddSeconds(30).ToString("O"),
            CreatedAt = DateTime.Now.ToString("O")
        });
    }

    /// <summary>
    /// 获取待重传记录
    /// </summary>
    public async Task<List<FailedCellRecord>> GetPendingAsync(int batchSize = 5)
    {
        const string sql = @"
            SELECT * FROM failed_cell_records
            WHERE NextRetryTime <= @Now
            ORDER BY NextRetryTime ASC
            LIMIT @BatchSize";

        var result = await SafeQueryAsync(sql, new
        {
            Now = DateTime.Now.ToString("O"),
            BatchSize = batchSize
        });

        return result.ToList();
    }

    /// <summary>
    /// 更新重试信息
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
    /// 删除已成功的记录
    /// </summary>
    public async Task DeleteAsync(long id)
    {
        await SafeExecuteAsync(
            $"DELETE FROM {TableName} WHERE Id = @Id",
            new { Id = id });
    }

    /// <summary>
    /// 获取当前失败记录总数（UI监控用）
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        return await SafeCountAsync($"SELECT COUNT(*) FROM {TableName}");
    }

    /// <summary>
    /// 重置所有 Abandoned 记录为可重试
    /// 将超过最大重试次数（NextRetryTime = DateTime.MaxValue）的记录重置
    /// </summary>
    public async Task ResetAllAbandonedAsync()
    {
        const string sql = @"
            UPDATE failed_cell_records
            SET RetryCount = 0,
                NextRetryTime = @Now
            WHERE NextRetryTime = @MaxTime";

        await SafeExecuteAsync(sql, new
        {
            Now = DateTime.Now.ToString("O"),
            MaxTime = DateTime.MaxValue.ToString("O")
        });
    }


}