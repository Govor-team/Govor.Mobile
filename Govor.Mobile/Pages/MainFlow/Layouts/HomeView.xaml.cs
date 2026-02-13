using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Govor.Mobile.PageModels.MainFlow;

namespace Govor.Mobile.Pages.MainFlow.Layouts;

public partial class HomeView : ContentView
{
    public HomeView()
    {
        InitializeComponent();
    }
    
    
    /* protected async override void OnAppearing()
    {
        base.OnAppearing();
        
        if(BindingContext is MainPageModel vm)
        {   
            if(vm.IsLoaded == false)
                await vm.InitAsync();
        }
    }*/

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView cv)
            cv.SelectedItem = null;
    }
}