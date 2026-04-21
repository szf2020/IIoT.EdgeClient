namespace IIoT.Edge.Module.ScanCaptureStarter.Constants;

public static class StarterModuleConstants
{
    public const string ModuleId = "ScanCaptureStarter";
    public const string ProcessType = "ScanCaptureStarter";

    public const string ScanTaskName = "ScanCaptureStarter.LoadingScan";
    public const string SignalTaskName = "ScanCaptureStarter.SignalCapture";

    public const string RuntimeRegisteredKey = "ScanCaptureStarter.RuntimeTaskRegistered";
    public const string LastObservedSequenceKey = "ScanCaptureStarter.LastObservedSequence";
    public const string LastObservedResultCodeKey = "ScanCaptureStarter.LastObservedResultCode";
    public const string LastObservedAtKey = "ScanCaptureStarter.LastObservedAt";
    public const string LastScannedBarcodeKey = "ScanCaptureStarter.LastScannedBarcode";
    public const string LastScannedAtKey = "ScanCaptureStarter.LastScannedAt";
    public const string PendingBarcodeKey = "ScanCaptureStarter.PendingBarcode";
    public const string PendingBarcodeObservedAtKey = "ScanCaptureStarter.PendingBarcodeObservedAt";
    public const string LastPublishedSequenceKey = "ScanCaptureStarter.LastPublishedSequence";
    public const string LastPublishedBarcodeKey = "ScanCaptureStarter.LastPublishedBarcode";
    public const string LastPublishedAtKey = "ScanCaptureStarter.LastPublishedAt";

    public const string CloudUploadEnabledKey = "ScanCaptureStarter.CloudUploadEnabled";
    public const string LastCloudUploadStatusKey = "ScanCaptureStarter.LastCloudUploadStatus";
    public const string LastCloudUploadAtKey = "ScanCaptureStarter.LastCloudUploadAt";
    public const string LastCloudUploadErrorKey = "ScanCaptureStarter.LastCloudUploadError";

    public const string CloudUploadSuccessStatus = "Success";
    public const string CloudUploadFailedStatus = "Failed";
    public const string CloudUploadDisabledStatus = "Disabled";
}
