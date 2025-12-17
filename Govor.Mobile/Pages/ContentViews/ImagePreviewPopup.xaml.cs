using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UXDivers.Popups.Maui;

namespace Govor.Mobile.Pages.ContentViews;

public partial class ImagePreviewPopup : PopupPage
{
    public ImagePreviewPopup(ImageSource src)
    {
        InitializeComponent();
        
        PreviewImage.Source = src;
    }

    private void RotateClicked(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }

    private void SaveClicked(object? sender, EventArgs e)
    {
        throw new NotImplementedException();
    }
}