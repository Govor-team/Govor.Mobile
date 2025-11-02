using CommunityToolkit.Mvvm.Messaging;

namespace Govor.Mobile.Pages.AuthFlow;

public partial class CodeInputPage : ContentPage
{
    private BrowserBottomSheet _browserControl;
    public CodeInputPage(CodeInputModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }


    private async void InvitationCode_Completed(object sender, EventArgs e)
    {
        if (BindingContext is CodeInputModel vm)
        {
            if (vm.RegisterCommand.CanExecute(null))
                await vm.RegisterCommand.ExecuteAsync(null);
        }
    }
}