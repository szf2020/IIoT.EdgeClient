using IIoT.Edge.Common.DataPipeline.Capacity;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline.Stores;
using IIoT.Edge.Infrastructure.Dapper.Connection;
using IIoT.Edge.Infrastructure.Dapper.Repository;
using Dapper;

namespace IIoT.Edge.Infrastructure.Dapper.Stores;

/// <summary>
/// 产能离线缓冲 SQLite 实现
/// 
/// 角色：离线临时缓冲，不是长期存储
///   - Offline 时 CapacityConsumer 写入
///   - Online 后 CapacitySyncTask 汇总 POST 云端 → 成功后清空
/// 
/// 对应数据库：pipeline.db
/// 对应表：capacity_buffer
/// </summary>
public class CapacityBufferStore : DapperRepositoryBase<CapacityRecord>, ICapacityBufferStore
{
    public override string DbName => "pipeline";
    protected override string TableName => "capacity_buffer";

    protected override string CreateTableSql => @"
        CREATE TABLE IF NOT EXISTS capacity_buffer (
            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
            Barcode         TEXT    NOT NULL,
            CellResult      INTEGER NOT NULL,
            ShiftCode       TEXT    NOT NULL,
            CompletedTime   TEXT    NOT NULL,
            CreatedAt       TEXT    NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_buffer_completed
            ON capacity_buffer (CompletedTime);
    ";

    public CapacityBufferStore(
        SqliteConnectionFactory connectionFactory,
        ILogService logger)
        : base(connectionFactory, logger)
    {
    }

    /// <summary>
    /// 插入一条离线缓冲记录
    /// </summary>
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
            record.ShiftCode,
            CompletedTime = record.CompletedTime.ToString("O"),
            CreatedAt = DateTime.Now.ToString("O")
        });
    }

    /// <summary>
    /// 按班次汇总（补传时用）
    /// </summary>
    public async Task<List<BufferSummaryDto>> GetShiftSummaryAsync()
    {
        const string sql = @"
            SELECT 
                substr(CompletedTime, 1, 10) AS Date,
                ShiftCode,
                COUNT(*) AS Total,
                SUM(CASE WHEN CellResult = 1 THEN 1 ELSE 0 END) AS OkCount,
                SUM(CASE WHEN CellResult = 0 THEN 1 ELSE 0 END) AS NgCount
            FROM capacity_buffer
            GROUP BY substr(CompletedTime, 1, 10), ShiftCode
            ORDER BY Date ASC, ShiftCode ASC";

        var result = await SafeQuerySummaryAsync(sql);
        return result.ToList();
    }

    /// <summary>
    /// 按日期+小时+班次汇总（小时补传）
    /// </summary>
    public async Task<List<BufferHourlySummaryDto>> GetHourlySummaryAsync()
    {
        const string sql = @"
            SELECT
                substr(CompletedTime, 1, 10) AS Date,
                CAST(substr(CompletedTime, 12, 2) AS INTEGER) AS Hour,
                CASE
                    WHEN CAST(substr(CompletedTime, 15, 2) AS INTEGER) >= 30 THEN 30
                    ELSE 0
                END AS MinuteBucket,
                ShiftCode,
                COUNT(*) AS Total,
                SUM(CASE WHEN CellResult = 1 THEN 1 ELSE 0 END) AS OkCount,
                SUM(CASE WHEN CellResult = 0 THEN 1 ELSE 0 END) AS NgCount
            FROM capacity_buffer
            GROUP BY
                substr(CompletedTime, 1, 10),
                CAST(substr(CompletedTime, 12, 2) AS INTEGER),
                CASE
                    WHEN CAST(substr(CompletedTime, 15, 2) AS INTEGER) >= 30 THEN 30
                    ELSE 0
                END,
                ShiftCode
            ORDER BY Date ASC, Hour ASC, MinuteBucket ASC, ShiftCode ASC";

        var result = await SafeQueryHourlySummaryAsync(sql);
        return result.ToList();
    }

    /// <summary>
    /// 补传成功后清空所有缓冲
    /// </summary>
    public async Task ClearAllAsync()
    {
        await SafeExecuteAsync($"DELETE FROM {TableName}");
    }

    /// <summary>
    /// 缓冲区记录数（诊断用）
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        return await SafeCountAsync($"SELECT COUNT(*) FROM {TableName}");
    }

    private async Task<IEnumerable<BufferSummaryDto>> SafeQuerySummaryAsync(
        string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<BufferSummaryDto>(sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 查询失败 [{TableName}]: {ex.Message}");
            return Enumerable.Empty<BufferSummaryDto>();
        }
    }

    private async Task<IEnumerable<BufferHourlySummaryDto>> SafeQueryHourlySummaryAsync(
        string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<BufferHourlySummaryDto>(sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 查询失败 [{TableName}]: {ex.Message}");
            return Enumerable.Empty<BufferHourlySummaryDto>();
        }
    }
}