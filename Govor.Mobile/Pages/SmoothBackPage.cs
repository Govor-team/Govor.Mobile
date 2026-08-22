using Microsoft.Maui.ApplicationModel;

namespace Govor.Mobile.Pages;

public partial class SmoothBackPage : ContentPage
{
    private bool _isGoingBack;

    protected override bool OnBackButtonPressed()
    {
#if ANDROID
        if (_isGoingBack)
            return true;

        _isGoingBack = true;

        Dispatcher.Dispatch(async () =>
        {
            try
            {
                await Shell.Current.GoToAsync("..", false);
            }
            catch
            {
                // swallow navigation errors
            }
            finally
            {
                _isGoingBack = false;
            }
        });


        return true;
#else
        return base.OnBackButtonPressed();
#endif
    }
}