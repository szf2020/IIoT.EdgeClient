using IIoT.Edge.Runtime.Context;
using IIoT.Edge.SharedKernel.DataPipeline.CellData;

namespace IIoT.Edge.NonUiRegressionTests;

public sealed class ProductionContextStorePersistenceTests
{
    [Fact]
    public void SaveAndLoad_ShouldRestoreCoreRuntimeState()
    {
        CellDataTypeRegistry.Register<InjectionCellData>("Injection");
        var tempDir = Path.Combine(Path.GetTempPath(), "edge-context-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var logger = new FakeLogService();
            var source = new ProductionContextStore(logger, tempDir);

            var ctx = source.GetOrCreate("PLC-A");
            ctx.SetStep("Scan", 3);
            ctx.Set("CurrentRecipeId", 42);
            ctx.AddCell("BC-1001", new InjectionCellData
            {
                Barcode = "BC-1001",
                WorkOrderNo = "WO-001",
                ScanTime = DateTime.UtcNow,
                InjectionVolume = 1.25
            });
            ctx.TodayCapacity.Increment(
                new DateTime(2026, 1, 2, 9, 0, 0),
                isOk: true,
                dayStart: new TimeSpan(8, 0, 0),
                dayEnd: new TimeSpan(20, 0, 0));

            source.SaveToFile();

            var restored = new ProductionContextStore(logger, tempDir);
            restored.LoadFromFile();

            var reloaded = restored.GetOrCreate("PLC-A");
            Assert.Equal(3, reloaded.GetStep("Scan"));
            Assert.Equal(42, reloaded.Get<int>("CurrentRecipeId"));
            Assert.True(reloaded.HasCell("BC-1001"));
            Assert.Equal(1, reloaded.TodayCapacity.TotalAll);
            Assert.Equal(1, reloaded.TodayCapacity.OkAll);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void LoadFromFile_WhenNoFile_ShouldKeepEmptyState()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "edge-context-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            var logger = new FakeLogService();
            var store = new ProductionContextStore(logger, tempDir);

            store.LoadFromFile();

            Assert.Empty(store.GetAll());
            Assert.Contains(logger.Entries, x => x.Message.Contains("No persisted file", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }
    }
}
