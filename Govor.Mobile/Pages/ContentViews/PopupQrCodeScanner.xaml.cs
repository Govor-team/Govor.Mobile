using Govor.Mobile.PageModels.ContentViewsModel;
using UXDivers.Popups.Maui;
using ZXing.Net.Maui;

namespace Govor.Mobile.Pages.ContentViews;

public partial class PopupQrCodeScanner : PopupPage
{
    private bool _isProcessing;

    public PopupQrCodeScanner(PopupQrCodeScannerModel vm)
	{
		InitializeComponent();
		BindingContext = vm;

        BarcodeReader.Options = new BarcodeReaderOptions
        {
            Formats = BarcodeFormats.All,
            AutoRotate = true,
            Multiple = false,
            TryHarder = true,

            DelayBetweenAnalyzingFrames = 150,
            InitialDelayBeforeAnalyzingFrames = 300,
            DelayBetweenContinuousScans = 1000
        };
    }

    private async void BarcodeReader_BarcodesDetected(
        object sender,
        BarcodeDetectionEventArgs e)
    {
        if (_isProcessing)
            return;

        var barcode = e.Results.FirstOrDefault();

        if (barcode == null)
            return;

        var value = barcode.Value;

        if (string.IsNullOrWhiteSpace(value))
            return;

        _isProcessing = true;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            StatusLabel.Text = value;

            if(BindingContext is PopupQrCodeScannerModel vm)
            {
                await vm.HandleQrCodeCommand.ExecuteAsync(value);
            }
        });
    }
}