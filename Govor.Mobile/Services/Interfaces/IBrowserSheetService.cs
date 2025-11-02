using System;
using System.Collections.Generic;
using System.Text;

namespace Govor.Mobile.Services.Interfaces
{
    public interface IBrowserSheetService
    {
        public void Initialize(BrowserBottomSheet sheet);
        public void Open(string url);
        public void Close();
    }
}
