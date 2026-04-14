using Dapper;
using IIoT.Edge.SharedKernel.DataPipeline.Capacity;
using IIoT.Edge.Application.Abstractions.Logging;
using IIoT.Edge.Application.Abstractions.DataPipeline.Stores;
using IIoT.Edge.Infrastructure.Persistence.Dapper.Connection;
using IIoT.Edge.Infrastructure.Persistence.Dapper.Repository;

namespace IIoT.Edge.Infrastructure.Persistence.Dapper.Stores;

/// <summary>
/// 产能离线缓冲的 SQLite 实现。
/// 用于离线场景下的缓冲，不作为长期存储。
/// </summary>
public class CapacityBufferStore : DapperRepositoryBase<CapacityRecord>, ICapacityBufferStore
{
    public override string DbName => "pipeline";
    protected override string TableName => "capacity_buffer";

    protected override string CreateTableSql => @"
        CREATE TABLE IF NOT EXISTS capacity_buffer (
            Id            INTEGER PRIMARY KEY AUTOINCREMENT,
            Barcode       TEXT    NOT NULL,
            CellResult    INTEGER NOT NULL,
            ShiftCode     TEXT    NOT NULL,
            CompletedTime TEXT    NOT NULL,
            CreatedAt     TEXT    NOT NULL,
            PlcName       TEXT    NOT NULL DEFAULT ''

        );
        CREATE INDEX IF NOT EXISTS idx_buffer_completed
            ON capacity_buffer (CompletedTime);
        CREATE INDEX IF NOT EXISTS idx_buffer_plcname
            ON capacity_buffer (PlcName);"
;

    public CapacityBufferStore(
        SqliteConnectionFactory connectionFactory,
        ILogService logger)
        : base(connectionFactory, logger)
    {
    }

    public async Task SaveAsync(CapacityRecord record)
    {
        const string sql = @"
            INSERT INTO capacity_buffer
                (Barcode, CellResult, ShiftCode, CompletedTime, CreatedAt, PlcName)
            VALUES
                (@Barcode, @CellResult, @ShiftCode, @CompletedTime, @CreatedAt, @PlcName)"
;

        await SafeExecuteAsync(sql, new
        {
            record.Barcode,
            record.CellResult,
            CompletedTime = record.CompletedTime.ToString("O"),
            record.ShiftCode,
            CreatedAt = DateTime.Now.ToString("O"),
            record.PlcName

        });
    }

    public async Task SaveBatchAsync(IEnumerable<CapacityRecord> records)
    {
        const string sql = @"
            INSERT INTO capacity_buffer
                (Barcode, CellResult, ShiftCode, CompletedTime, CreatedAt, PlcName)
            VALUES
                (@Barcode, @CellResult, @ShiftCode, @CompletedTime, @CreatedAt, @PlcName)"
;

        var now = DateTime.Now.ToString("O");
        var rows = records.Select(r => new
        {
            r.Barcode,
            r.CellResult,
            r.ShiftCode,
            CompletedTime = r.CompletedTime.ToString("O"),
            CreatedAt = now,
            r.PlcName

        }).ToList();

        if (rows.Count == 0) return;

        try
        {
            await ExecuteInTransactionAsync<int>(async (conn, tx) =>
            {
                await conn.ExecuteAsync(sql, rows, transaction: tx, commandTimeout: CommandTimeout);
                return 0;
            });
            Logger.Info($"[CapacityBuffer] 批量写入 {rows.Count} 条完成");
        }
        catch (Exception ex)
        {
            Logger.Error($"[CapacityBuffer] 批量写入失败: {ex.Message}");
        }
    }

    public async Task<List<BufferSummaryDto>> GetShiftSummaryAsync()
    {
        const string sql = @"
            SELECT
                substr(CompletedTime, 1, 10)                     AS Date,
                ShiftCode,
                COUNT(*)                                         AS Total,
                SUM(CASE WHEN CellResult = 1 THEN 1 ELSE 0 END) AS OkCount,
                SUM(CASE WHEN CellResult = 0 THEN 1 ELSE 0 END) AS NgCount
            FROM capacity_buffer
            GROUP BY substr(CompletedTime, 1, 10), ShiftCode
            ORDER BY Date ASC, ShiftCode ASC";

        return await SafeQueryAsync<BufferSummaryDto>(sql);
    }

    public async Task<List<BufferHourlySummaryDto>> GetHourlySummaryAsync()
    {
        const string sql = @"
            SELECT
                substr(CompletedTime, 1, 10)                          AS Date,
                CAST(substr(CompletedTime, 12, 2) AS INTEGER)         AS Hour,
                CASE
                    WHEN CAST(substr(CompletedTime, 15, 2) AS INTEGER) >= 30
                    THEN 30 ELSE 0
                END                                                   AS MinuteBucket,
                ShiftCode,
                PlcName,
                COUNT(*)                                              AS Total,
                SUM(CASE WHEN CellResult = 1 THEN 1 ELSE 0 END)      AS OkCount,
                SUM(CASE WHEN CellResult = 0 THEN 1 ELSE 0 END)      AS NgCount
            FROM capacity_buffer
            GROUP BY
                substr(CompletedTime, 1, 10),
                CAST(substr(CompletedTime, 12, 2) AS INTEGER),
                CASE
                    WHEN CAST(substr(CompletedTime, 15, 2) AS INTEGER) >= 30
                    THEN 30 ELSE 0
                END,
                ShiftCode,
                PlcName

            ORDER BY Date ASC, Hour ASC, MinuteBucket ASC, ShiftCode ASC";

        return await SafeQueryAsync<BufferHourlySummaryDto>(sql);
    }

    public async Task ClearAllAsync()
        => await SafeExecuteAsync($"DELETE FROM {TableName}");

    public async Task<int> GetCountAsync()
        => await SafeCountAsync($"SELECT COUNT(*) FROM {TableName}");

    private async Task<List<T>> SafeQueryAsync<T>(string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            var result = await conn.QueryAsync<T>(sql, param, commandTimeout: CommandTimeout);
            return result.ToList();
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 查询失败 [{TableName}]: {ex.Message}");
            return new List<T>();
        }
    }
}

