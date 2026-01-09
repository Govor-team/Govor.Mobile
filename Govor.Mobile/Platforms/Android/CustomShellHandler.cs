using Android.Graphics.Drawables;
using Android.Views;
using Android.Widget;
using Google.Android.Material.BottomNavigation;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform;
using Microsoft.Maui.Controls.Platform.Compatibility;
using Microsoft.Maui.Platform;

namespace Govor.Mobile.FloatingTabBar;

internal class RoundedFloatingTabBarHandler : ShellRenderer
{
    protected override IShellBottomNavViewAppearanceTracker CreateBottomNavViewAppearanceTracker(ShellItem shellItem)
    {
        return new RoundedFtoatingBottomNavViewAppearanceTracker(this, shellItem);
    }
}


internal class RoundedFtoatingBottomNavViewAppearanceTracker : ShellBottomNavViewAppearanceTracker
{
    public RoundedFtoatingBottomNavViewAppearanceTracker(IShellContext shellContext, ShellItem shellItem) 
        : base(shellContext, shellItem)
    {
    }

    public override void SetAppearance(BottomNavigationView bottomView, IShellAppearanceElement appearance)
    {
        base.SetAppearance(bottomView, appearance);

        // Полностью убираем фон у всех нативных контейнеров навигации
        bottomView.SetBackgroundColor(Android.Graphics.Color.Transparent);

        if (bottomView.Parent is ViewGroup parent)
        {
            parent.SetBackgroundColor(Android.Graphics.Color.Transparent);

            // Рекурсивно поднимаемся выше, чтобы убрать белые подложки Shell
            var grandParent = parent.Parent as ViewGroup;
            while (grandParent != null && grandParent.Id != Android.Resource.Id.Content)
            {
                grandParent.SetBackgroundColor(Android.Graphics.Color.Transparent);
                grandParent = grandParent.Parent as ViewGroup;
            }
        }

        // 2. Создаем вашу плавающую желтую панель
        var gradientDrawable = new GradientDrawable();
        gradientDrawable.SetShape(ShapeType.Rectangle);
        gradientDrawable.SetCornerRadius(70);
        gradientDrawable.SetColor(appearance.EffectiveTabBarBackgroundColor.ToPlatform());

        bottomView.SetBackground(gradientDrawable);

        // 3. Отступы, чтобы панель "парила" над фоновым изображением
        if (bottomView.LayoutParameters is ViewGroup.MarginLayoutParams marginParams)
        {
            marginParams.SetMargins(50, 0, 50, 60);
            bottomView.LayoutParameters = marginParams;
        }
    }
}