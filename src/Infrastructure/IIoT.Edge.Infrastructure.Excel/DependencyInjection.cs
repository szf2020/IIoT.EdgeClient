using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using IIoT.Edge.Contracts.DataPipeline.Consumers;
using IIoT.Edge.Infrastructure.Excel.Consumers;
using Microsoft.Extensions.DependencyInjection;

namespace IIoT.Edge.Infrastructure.Excel;

public static class DependencyInjection
{
    /// <summary>
    /// 注册 Excel 基础设施
    /// 
    /// 在 Shell DependencyInjection 中调用：
    ///   var excelDir = Path.Combine(appDataDir, "excel");
    ///   services.AddExcelInfrastructure(excelDir);
    /// </summary>
    public static IServiceCollection AddExcelInfrastructure(
        this IServiceCollection services,
        string excelDirectory)
    {
        // 注册为 IExcelConsumer（单独访问）+ ICellDataConsumer（队列统一调度）
        services.AddSingleton<IExcelConsumer>(sp =>
            new ExcelConsumer(excelDirectory, sp.GetRequiredService<ILogService>()));
        services.AddSingleton<ICellDataConsumer>(sp =>
            sp.GetRequiredService<IExcelConsumer>());

        return services;
    }
}