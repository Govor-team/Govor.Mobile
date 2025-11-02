using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

class BrowserSheetService : IBrowserSheetService
{
    private BrowserBottomSheet? _sheet;

    public void Initialize(BrowserBottomSheet sheet)
    {
        _sheet = sheet;
    }

    public void Open(string url)
    {
        _sheet?.Open(url);
    }

    public void Close()
    {
        //_sheet?.Close();
    }
}
