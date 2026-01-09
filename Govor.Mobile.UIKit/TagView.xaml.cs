using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Govor.Mobile.UIKit;

public partial class TagView : ContentView
{
    private bool _isTooltipVisible;

    public TagView()
    {
        InitializeComponent();
            /*
#if WINDOWS || MACCATALYST
        var pointer = new PointerGestureRecognizer();
        pointer.PointerEntered += (_, _) => ShowTooltip();
        pointer.PointerExited += (_, _) => HideTooltip();
        GestureRecognizers.Add(pointer);
#else
        var tap = new TapGestureRecognizer();
        tap.Tapped += OnTapped;
        GestureRecognizers.Add(tap);
#endif  */
    }

    private async void OnTapped(object sender, EventArgs e)
    {
        if (_isTooltipVisible)
        {
            HideTooltip();
        }
        else
        {
            ShowTooltip();
        }
    }

    private async void ShowTooltip()
    {
        if (_isTooltipVisible)
            return;

        _isTooltipVisible = true;
        Tooltip.IsVisible = true;

        await Task.WhenAll(
            Tooltip.FadeTo(1, 120),
            Tooltip.TranslateTo(0, -44, 120),
            TagBody.ScaleTo(1.05, 120)
        );
    }

    private async void HideTooltip()
    {
        if (!_isTooltipVisible)
            return;

        _isTooltipVisible = false;

        await Task.WhenAll(
            Tooltip.FadeTo(0, 120),
            Tooltip.TranslateTo(0, -36, 120),
            TagBody.ScaleTo(1.0, 120)
        );

        Tooltip.IsVisible = false;
    }
}

