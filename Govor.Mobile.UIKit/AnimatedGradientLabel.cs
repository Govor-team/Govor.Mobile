using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Govor.Mobile.UIKit;

public class AnimatedGradientLabel : SKCanvasView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(AnimatedGradientLabel), "Tag", propertyChanged: (b, o, n) => ((AnimatedGradientLabel)b).InvalidateSurface());
    public static readonly BindableProperty FontSizeProperty = BindableProperty.Create(nameof(FontSize), typeof(float), typeof(AnimatedGradientLabel), 14f);
    public static readonly BindableProperty ColorsProperty = BindableProperty.Create(nameof(Colors), typeof(Color[]), typeof(AnimatedGradientLabel), new[] { Microsoft.Maui.Graphics.Colors.Gold, Microsoft.Maui.Graphics.Colors.White, Microsoft.Maui.Graphics.Colors.Gold });

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public float FontSize { get => (float)GetValue(FontSizeProperty); set => SetValue(FontSizeProperty, value); }
    public Color[] Colors { get => (Color[])GetValue(ColorsProperty); set => SetValue(ColorsProperty, value); }

    private float _animationPhase = 0;

    public AnimatedGradientLabel()
    {
        // Запуск бесконечной анимации
        var animation = new Animation(v => 
        {
            _animationPhase = (float)v;
            InvalidateSurface(); // Перерисовка кадра
        }, 0, 1);
        
        animation.Commit(this, "GradientAnimation", length: 2000, repeat: () => true);
    }

    protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear();

        var skColors = Colors.Select(c => c.ToSKColor()).ToArray();
        
        using var paint = new SKPaint
        {
            TextSize = FontSize * (float)e.Info.Width / (float)this.Width,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold),
            TextAlign = SKTextAlign.Left
        };

        // Измеряем текст для центрирования
        var textBounds = new SKRect();
        paint.MeasureText(Text, ref textBounds);
        float y = (e.Info.Height / 2) - textBounds.MidY;

        // Создаем движущийся градиент (Shader)
        float gradientWidth = e.Info.Width * 0.8f;
        float xOffset = (e.Info.Width + gradientWidth) * _animationPhase - gradientWidth;

        paint.Shader = SKShader.CreateLinearGradient(
            new SKPoint(xOffset, 0),
            new SKPoint(xOffset + gradientWidth, 0),
            skColors,
            null,
            SKShaderTileMode.Clamp);

        canvas.DrawText(Text, 0, y, paint);
    }
}