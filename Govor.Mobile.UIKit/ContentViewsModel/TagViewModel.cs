using CommunityToolkit.Mvvm.ComponentModel;

namespace Govor.Mobile.UIKit.ContentViewsModel;

public sealed partial class TagViewModel : ObservableObject
{
    [ObservableProperty] 
    private ImageSource icon;
    
    [ObservableProperty]
    private string text;

    [ObservableProperty]
    private string description;

    [ObservableProperty]
    private Color bodyColor = Colors.Transparent;
    
    [ObservableProperty]
    private Color textColor = Colors.White;

    [ObservableProperty]
    private Color strokeColor = Colors.Transparent;

    [ObservableProperty]
    private bool isVisible = true;
    
    [ObservableProperty]
    private Shadow customShadow;
    
    public void SetSolidShadow(Color color)
    {
        CustomShadow = new Shadow
        {
            Brush = new SolidColorBrush(color),
            Offset = new Point(0, 5),
            Radius = 10,
            Opacity = 0.3f
        };
    }
    
    public void SetGradientShadow(Point startPoint, Point endPoint, params Color[] colors)
    {
        if (colors == null || colors.Length == 0)
            return;

        var gradients = new GradientStopCollection();

        for (int i = 0; i < colors.Length; i++)
        {
            float offset = (float)i / (colors.Length - 1 >= 1 ? colors.Length - 1 : 1);
            gradients.Add(new GradientStop(colors[i], offset));
        }

        CustomShadow = new Shadow
        {
            Brush = new LinearGradientBrush
            {
                GradientStops = gradients,
                StartPoint = startPoint, 
                EndPoint = endPoint      
            },
            Offset = new Point(0, 8),
            Radius = 15,
            Opacity = 0.3f
        };
    }
}
