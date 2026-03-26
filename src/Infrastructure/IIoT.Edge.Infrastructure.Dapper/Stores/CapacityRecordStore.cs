using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Infrastructure.Dapper.Connection;
using IIoT.Edge.Infrastructure.Dapper.Repository;
using Dapper;

namespace IIoT.Edge.Infrastructure.Dapper.Stores;

/// <summary>
/// 产能记录的 SQLite 存储实现
/// 
/// 对应数据库：pipeline.db
/// 对应表：capacity_records
/// </summary>
public class CapacityRecordStore : DapperRepositoryBase<CapacityRecord>, ICapacityRecordStore
{
    public override string DbName => "pipeline";
    protected override string TableName => "capacity_records";

    protected override string CreateTableSql => @"
        CREATE TABLE IF NOT EXISTS capacity_records (
            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
            Barcode         TEXT    NOT NULL,
            CellResult      INTEGER NOT NULL,
            ShiftCode       TEXT    NOT NULL,
            CompletedTime   TEXT    NOT NULL,
            CreatedAt       TEXT    NOT NULL
        );

        CREATE INDEX IF NOT EXISTS idx_capacity_completed
            ON capacity_records (CompletedTime);

        CREATE INDEX IF NOT EXISTS idx_capacity_barcode
            ON capacity_records (Barcode);
    ";

    public CapacityRecordStore(
        SqliteConnectionFactory connectionFactory,
        ILogService logger)
        : base(connectionFactory, logger)
    {
    }

    /// <summary>
    /// 插入一条产能记录
    /// </summary>
    public async Task SaveAsync(CapacityRecord record)
    {
        const string sql = @"
            INSERT INTO capacity_records
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
    /// 按天汇总（产能查询页面表格用）
    /// </summary>
    public async Task<List<DailySummaryDto>> GetDailySummaryAsync(
        DateTime dateFrom, DateTime dateTo)
    {
        const string sql = @"
            SELECT 
                substr(CompletedTime, 1, 10) AS Date,
                COUNT(*) AS Total,
                SUM(CASE WHEN CellResult = 1 THEN 1 ELSE 0 END) AS OkCount,
                SUM(CASE WHEN CellResult = 0 THEN 1 ELSE 0 END) AS NgCount
            FROM capacity_records
            WHERE CompletedTime >= @DateFrom
              AND CompletedTime < @DateTo
            GROUP BY substr(CompletedTime, 1, 10)
            ORDER BY Date ASC";

        var result = await SafeQueryAsync<DailySummaryDto>(sql, new
        {
            DateFrom = dateFrom.ToString("O"),
            DateTo = dateTo.AddDays(1).ToString("O")
        });

        return result.ToList();
    }

    /// <summary>
    /// 区间汇总（顶部卡片用）
    /// </summary>
    public async Task<DailySummaryDto> GetPeriodSummaryAsync(
        DateTime dateFrom, DateTime dateTo)
    {
        const string sql = @"
            SELECT 
                '' AS Date,
                COUNT(*) AS Total,
                SUM(CASE WHEN CellResult = 1 THEN 1 ELSE 0 END) AS OkCount,
                SUM(CASE WHEN CellResult = 0 THEN 1 ELSE 0 END) AS NgCount
            FROM capacity_records
            WHERE CompletedTime >= @DateFrom
              AND CompletedTime < @DateTo";

        var result = await SafeQueryFirstOrDefaultAsync<DailySummaryDto>(sql, new
        {
            DateFrom = dateFrom.ToString("O"),
            DateTo = dateTo.AddDays(1).ToString("O")
        });

        return result ?? new DailySummaryDto("", 0, 0, 0);
    }

    /// <summary>
    /// 总记录数
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        return await SafeCountAsync($"SELECT COUNT(*) FROM {TableName}");
    }

    /// <summary>
    /// 泛型查询（DailySummaryDto 不是 TEntity，需要单独的方法）
    /// </summary>
    private async Task<IEnumerable<T>> SafeQueryAsync<T>(string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.QueryAsync<T>(sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 查询失败 [{TableName}]: {ex.Message}");
            return Enumerable.Empty<T>();
        }
    }

    private async Task<T?> SafeQueryFirstOrDefaultAsync<T>(string sql, object? param = null)
    {
        try
        {
            using var conn = GetConnection();
            return await conn.QueryFirstOrDefaultAsync<T>(sql, param, commandTimeout: CommandTimeout);
        }
        catch (Exception ex)
        {
            Logger.Error($"[Dapper] 查询失败 [{TableName}]: {ex.Message}");
            return default;
        }
    }
}