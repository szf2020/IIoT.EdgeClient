using ClosedXML.Excel;
using IIoT.Edge.Common.DataPipeline;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.DataPipeline;
using System.Text.Json;

namespace IIoT.Edge.Infrastructure.Excel.Consumers;

/// <summary>
/// Excel 本地存储消费者（默认实现）
/// 
/// 将电芯数据追加写入本地 Excel 文件
/// 按天生成文件：2026-03-25_生产数据.xlsx
/// 列头根据 DataJson 动态生成，不同机台自动适配
/// 
/// 固定列（始终在最前面）：
///   条码 | 设备名称 | 结果 | 完成时间
/// 
/// 动态列（根据 DataJson 的 key 自动生成）：
///   scan.time | scan.source | voltage.value | ...
/// </summary>
public class ExcelConsumer : IExcelConsumer
{
    private readonly string _excelDirectory;
    private readonly ILogService _logger;
    private readonly object _fileLock = new();

    private static readonly string[] FixedHeaders = ["条码", "设备名称", "结果", "完成时间"];

    public string Name => "Excel";
    public int Order => 30;

    public ExcelConsumer(string excelDirectory, ILogService logger)
    {
        _excelDirectory = excelDirectory;
        _logger = logger;
        Directory.CreateDirectory(_excelDirectory);
    }

    public Task<bool> ProcessAsync(CellCompletedRecord record)
    {
        try
        {
            var cellData = ParseDataJson(record.DataJson);

            var fileName = $"{record.CompletedTime:yyyy-MM-dd}_生产数据.xlsx";
            var filePath = Path.Combine(_excelDirectory, fileName);

            lock (_fileLock)
            {
                WriteToExcel(filePath, record, cellData);
            }

            _logger.Info($"[{record.DeviceName}] Excel写入成功，条码: {record.Barcode}");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.Error($"[{record.DeviceName}] Excel写入失败，条码: {record.Barcode}，{ex.Message}");
            return Task.FromResult(false);
        }
    }

    private void WriteToExcel(
        string filePath,
        CellCompletedRecord record,
        Dictionary<string, string> cellData)
    {
        var dynamicKeys = cellData.Keys.OrderBy(k => k).ToList();

        if (File.Exists(filePath))
        {
            using var workbook = new XLWorkbook(filePath);
            var worksheet = workbook.Worksheets.First();

            var existingHeaders = ReadHeaders(worksheet);

            // 检查是否有新的动态列需要追加
            var newKeys = dynamicKeys.Where(k => !existingHeaders.Contains(k)).ToList();
            if (newKeys.Count > 0)
            {
                var nextCol = existingHeaders.Count + 1;
                foreach (var key in newKeys)
                {
                    worksheet.Cell(1, nextCol).Value = key;
                    StyleHeaderCell(worksheet.Cell(1, nextCol));
                    existingHeaders.Add(key);
                    nextCol++;
                }
            }

            var nextRow = worksheet.LastRowUsed()!.RowNumber() + 1;
            WriteDataRow(worksheet, nextRow, record, cellData, existingHeaders);

            workbook.Save();
        }
        else
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.AddWorksheet("生产数据");

            var allHeaders = new List<string>(FixedHeaders);
            allHeaders.AddRange(dynamicKeys);

            for (int i = 0; i < allHeaders.Count; i++)
            {
                worksheet.Cell(1, i + 1).Value = allHeaders[i];
                StyleHeaderCell(worksheet.Cell(1, i + 1));
            }

            WriteDataRow(worksheet, 2, record, cellData, allHeaders);

            worksheet.Columns().AdjustToContents(1, 1, 50);

            workbook.SaveAs(filePath);
        }
    }

    private static void WriteDataRow(
        IXLWorksheet worksheet,
        int row,
        CellCompletedRecord record,
        Dictionary<string, string> cellData,
        List<string> headers)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            var header = headers[i];
            var col = i + 1;

            var value = header switch
            {
                "条码" => record.Barcode,
                "设备名称" => record.DeviceName,
                "结果" => record.CellResult ? "OK" : "NG",
                "完成时间" => record.CompletedTime.ToString("yyyy-MM-dd HH:mm:ss"),
                _ => cellData.TryGetValue(header, out var v) ? v : ""
            };

            worksheet.Cell(row, col).Value = value;

            if (header == "结果" && value == "NG")
            {
                worksheet.Cell(row, col).Style.Font.FontColor = XLColor.Red;
                worksheet.Cell(row, col).Style.Font.Bold = true;
            }
        }
    }

    private static List<string> ReadHeaders(IXLWorksheet worksheet)
    {
        var headers = new List<string>();
        var headerRow = worksheet.Row(1);

        for (int col = 1; col <= headerRow.LastCellUsed()!.Address.ColumnNumber; col++)
        {
            var value = headerRow.Cell(col).GetString();
            if (!string.IsNullOrEmpty(value))
                headers.Add(value);
        }

        return headers;
    }

    private static void StyleHeaderCell(IXLCell cell)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Fill.BackgroundColor = XLColor.LightGray;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private Dictionary<string, string> ParseDataJson(string dataJson)
    {
        var result = new Dictionary<string, string>();

        try
        {
            using var doc = JsonDocument.Parse(dataJson);

            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                var value = prop.Value.ValueKind switch
                {
                    JsonValueKind.String => prop.Value.GetString() ?? "",
                    JsonValueKind.Number => prop.Value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => "",
                    _ => prop.Value.GetRawText()
                };

                result[prop.Name] = value;
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"[Excel] DataJson 解析失败: {ex.Message}");
        }

        return result;
    }
}