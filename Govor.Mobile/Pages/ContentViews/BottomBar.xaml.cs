using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Govor.Mobile.PageModels.ContentViewsModel;

namespace Govor.Mobile.Pages.ContentViews;

public partial class BottomBar : ContentView
{
    public BottomBar()
    {
        InitializeComponent();
        BindingContext = new BottomBarModel();
    }
}