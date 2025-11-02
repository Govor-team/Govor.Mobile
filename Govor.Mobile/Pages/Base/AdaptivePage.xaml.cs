namespace Govor.Mobile.Pages.Base;

public partial class AdaptivePage : ContentPage
{
    /// <summary>
    /// Минимальная ширина окна для перехода в "широкий" режим.
    /// </summary>
    protected virtual double WideThreshold => 700;

    private bool _isWide = false;

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        // Если состояние изменилось — переключаемся
        bool nowWide = width > WideThreshold;

        if (nowWide != _isWide)
        {
            _isWide = nowWide;
            if (nowWide)
                OnSwitchToWide();
            else
                OnSwitchToNarrow();
        }
    }

    /// <summary>
    /// Срабатывает, когда ширина превышает WideThreshold.
    /// </summary>
    protected virtual void OnSwitchToWide() { }

    /// <summary>
    /// Срабатывает, когда ширина становится меньше WideThreshold.
    /// </summary>
    protected virtual void OnSwitchToNarrow() { }
}