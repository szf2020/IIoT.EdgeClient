using IIoT.Edge.Module.ScanCaptureStarter.Constants;
using IIoT.Edge.SharedKernel.DataPipeline.CellData;

namespace IIoT.Edge.Module.ScanCaptureStarter.Payload;

public sealed class StarterCellData : CellDataBase
{
    public override string ProcessType => StarterModuleConstants.ProcessType;

    public string Barcode { get; set; } = string.Empty;

    public int SequenceNo { get; set; }

    public string RuntimeStatus { get; set; } = string.Empty;

    public override string DisplayLabel => string.IsNullOrWhiteSpace(Barcode) ? ProcessType : Barcode;
}
