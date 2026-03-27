using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace IIoT.Edge.DataMapping.Cloud;

/// <summary>
/// 注液过站数据 → 云端 DTO 映射器
/// 
/// 职责：CellCompletedRecord.DataJson → ReceiveInjectionPassDto
/// 
/// DataJson 里的 key 名通过配置映射，换客户换机型只改 appsettings.json：
///   "FieldMapping:Injection": {
///     "PreInjectionTime": "preInjectionTime",
///     "PreInjectionWeight": "preInjectionWeight",
///     "PostInjectionTime": "postInjectionTime",
///     "PostInjectionWeight": "postInjectionWeight",
///     "InjectionVolume": "injectionVolume"
///   }
/// 
/// 不配就用默认值（驼峰名，和云端字段名一致）
/// </summary>
public class InjectionCloudMapper
{
    private readonly ILogService _logger;

    private readonly string _keyPreInjectionTime;
    private readonly string _keyPreInjectionWeight;
    private readonly string _keyPostInjectionTime;
    private readonly string _keyPostInjectionWeight;
    private readonly string _keyInjectionVolume;

    public InjectionCloudMapper(IConfiguration configuration, ILogService logger)
    {
        _logger = logger;

        var section = configuration.GetSection("FieldMapping:Injection");

        _keyPreInjectionTime = section["PreInjectionTime"] ?? "preInjectionTime";
        _keyPreInjectionWeight = section["PreInjectionWeight"] ?? "preInjectionWeight";
        _keyPostInjectionTime = section["PostInjectionTime"] ?? "postInjectionTime";
        _keyPostInjectionWeight = section["PostInjectionWeight"] ?? "postInjectionWeight";
        _keyInjectionVolume = section["InjectionVolume"] ?? "injectionVolume";
    }

    /// <summary>
    /// 将 CellCompletedRecord 转换为云端请求 DTO
    /// 转换失败返回 null
    /// </summary>
    public ReceiveInjectionPassDto? Map(CellCompletedRecord record, Guid cloudDeviceId)
    {
        try
        {
            using var doc = JsonDocument.Parse(record.DataJson);
            var root = doc.RootElement;

            return new ReceiveInjectionPassDto
            {
                DeviceId = cloudDeviceId,
                Barcode = record.Barcode,
                CellResult = record.CellResult ? "OK" : "NG",
                CompletedTime = record.CompletedTime,
                PreInjectionTime = GetDateTime(root, _keyPreInjectionTime, record.Barcode),
                PreInjectionWeight = GetDouble(root, _keyPreInjectionWeight, record.Barcode),
                PostInjectionTime = GetDateTime(root, _keyPostInjectionTime, record.Barcode),
                PostInjectionWeight = GetDouble(root, _keyPostInjectionWeight, record.Barcode),
                InjectionVolume = GetDouble(root, _keyInjectionVolume, record.Barcode)
            };
        }
        catch (JsonException ex)
        {
            _logger.Error($"[CloudMapper] DataJson 解析失败，条码: {record.Barcode}，{ex.Message}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error($"[CloudMapper] 字段映射失败，条码: {record.Barcode}，{ex.Message}");
            return null;
        }
    }

    private DateTime GetDateTime(JsonElement root, string key, string barcode)
    {
        if (root.TryGetProperty(key, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String
                && DateTime.TryParse(prop.GetString(), out var dt))
                return dt;
        }

        _logger.Warn($"[CloudMapper] 缺少或无法解析字段: {key}，条码: {barcode}");
        return DateTime.MinValue;
    }

    private double GetDouble(JsonElement root, string key, string barcode)
    {
        if (root.TryGetProperty(key, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number)
                return prop.GetDouble();

            if (prop.ValueKind == JsonValueKind.String
                && double.TryParse(prop.GetString(), out var val))
                return val;
        }

        _logger.Warn($"[CloudMapper] 缺少或无法解析字段: {key}，条码: {barcode}");
        return 0;
    }
}