using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Govor.Mobile.Pages;

public partial class ImagePreviewPage : ContentPage
{
    public ObservableCollection<string> Images { get; set; } // URLs или пути к изображениям
    public int CurrentIndex { get; set; }

    public ImagePreviewPage(List<string> images, int index)
    {
        InitializeComponent();
        BindingContext = this;
        Images = new ObservableCollection<string>(images);
        CurrentIndex = index;
    }
}