using Dapper;
using IIoT.Edge.Common.DataPipeline.Capacity;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Infrastructure.Dapper.Connection;
using IIoT.Edge.Infrastructure.Dapper.Repository;

namespace IIoT.Edge.Infrastructure.Dapper.Stores;

/// <summary>
/// 产能离线缓冲 SQLite 实现
///
/// 角色：离线临时缓冲，不是长期存储
///   - Offline 时 CapacityConsumer 单条写入
///   - 批量场景（历史数据生成）用 SaveBatchAsync 事务批量写入
///   - Online 后 CapacitySyncTask 聚合 POST 云端 → 成功后清空
///
/// 对应数据库：pipeline.db / 对应表：capacity_buffer
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
            CreatedAt     TEXT    NOT NULL
        );
        CREATE INDEX IF NOT EXISTS idx_buffer_completed
            ON capacity_buffer (CompletedTime);";

    public CapacityBufferStore(
        SqliteConnectionFactory connectionFactory,
        ILogService logger)
        : base(connectionFactory, logger)
    {
    }

    // ── 单条写入（实时，每个电芯完成时调用）─────────────────────────

    public async Task SaveAsync(CapacityRecord record)
    {
        const string sql = @"
            INSERT INTO capacity_buffer
                (Barcode, CellResult, ShiftCode, CompletedTime, CreatedAt)
            VALUES
                (@Barcode, @CellResult, @ShiftCode, @CompletedTime, @CreatedAt)";

        await SafeExecuteAsync(sql, new
        {
            record.Barcode,
            record.CellResult,
            CompletedTime = record.CompletedTime.ToString("O"),
            record.ShiftCode,
            CreatedAt = DateTime.Now.ToString("O")
        });
    }

    // ── 批量写入（事务，历史数据生成/大批量场景）────────────────────

    public async Task SaveBatchAsync(IEnumerable<CapacityRecord> records)
    {
        const string sql = @"
            INSERT INTO capacity_buffer
                (Barcode, CellResult, ShiftCode, CompletedTime, CreatedAt)
            VALUES
                (@Barcode, @CellResult, @ShiftCode, @CompletedTime, @CreatedAt)";

        var now = DateTime.Now.ToString("O");
        var rows = records.Select(r => new
        {
            r.Barcode,
            r.CellResult,
            r.ShiftCode,
            CompletedTime = r.CompletedTime.ToString("O"),
            CreatedAt = now
        }).ToList();

        if (rows.Count == 0) return;

        try
        {
            await ExecuteInTransactionAsync(async (conn, tx) =>
            {
                await conn.ExecuteAsync(sql, rows, transaction: tx, commandTimeout: CommandTimeout);
            });
            Logger.Info($"[CapacityBuffer] 批量写入 {rows.Count} 条完成");
        }
        catch (Exception ex)
        {
            Logger.Error($"[CapacityBuffer] 批量写入失败: {ex.Message}");
        }
    }

    // ── 按班次汇总（兼容旧补传）──────────────────────────────────────

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

    // ── 按半小时桶汇总（补传主链路）─────────────────────────────────

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
                ShiftCode
            ORDER BY Date ASC, Hour ASC, MinuteBucket ASC, ShiftCode ASC";

        return await SafeQueryAsync<BufferHourlySummaryDto>(sql);
    }

    // ── 清空 / 计数 ──────────────────────────────────────────────────

    public async Task ClearAllAsync()
        => await SafeExecuteAsync($"DELETE FROM {TableName}");

    public async Task<int> GetCountAsync()
        => await SafeCountAsync($"SELECT COUNT(*) FROM {TableName}");

    // ── 私有通用查询（支持泛型 DTO）─────────────────────────────────

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