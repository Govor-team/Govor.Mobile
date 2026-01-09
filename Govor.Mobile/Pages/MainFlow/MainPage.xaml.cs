using Govor.Mobile.PageModels.MainFlow;
using Govor.Mobile.Pages.Base;

namespace Govor.Mobile.Pages.MainFlow;

public partial class MainPage : AdaptivePage
{
    public MainPage(MainPageModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
    
    protected async override void OnAppearing()
    {
        base.OnAppearing();
        
        if(BindingContext is MainPageModel vm)
        {   
            if(vm.IsLoaded == false)
                await vm.InitAsync();
        }
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is CollectionView cv)
            cv.SelectedItem = null;
    }
}