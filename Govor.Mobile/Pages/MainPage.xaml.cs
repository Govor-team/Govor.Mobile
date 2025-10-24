using Govor.Mobile.Models;
using Govor.Mobile.PageModels;

namespace Govor.Mobile.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}