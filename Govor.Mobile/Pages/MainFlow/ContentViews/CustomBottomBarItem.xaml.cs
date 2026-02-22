using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Govor.Mobile.Pages.MainFlow.ContentViews;

public partial class CustomBottomBarItem : ContentView
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(CustomBottomBarItem), string.Empty);
    public static readonly BindableProperty IconProperty = BindableProperty.Create(nameof(Icon), typeof(ImageSource), typeof(CustomBottomBarItem));
    public static readonly BindableProperty BadgeCountProperty = BindableProperty.Create(nameof(BadgeCount), typeof(string), typeof(CustomBottomBarItem), string.Empty, propertyChanged: OnBadgeCountChanged);
    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(nameof(IsSelected), typeof(bool), typeof(CustomBottomBarItem), false);
    public static readonly BindableProperty ShowTitleProperty = BindableProperty.Create(nameof(ShowTitle), typeof(bool), typeof(CustomBottomBarItem), true);
    public static readonly BindableProperty TabIndexProperty = BindableProperty.Create(nameof(TabIndex), typeof(string), typeof(CustomBottomBarItem));
    public static readonly BindableProperty TabCommandProperty = BindableProperty.Create(nameof(TabCommand), typeof(ICommand), typeof(CustomBottomBarItem));
    public static readonly BindableProperty HasBadgeProperty = BindableProperty.Create(nameof(HasBadge), typeof(bool), typeof(CustomBottomBarItem), false);

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public ImageSource Icon { get => (ImageSource)GetValue(IconProperty); set => SetValue(IconProperty, value); }
    public string BadgeCount { get => (string)GetValue(BadgeCountProperty); set => SetValue(BadgeCountProperty, value); }
    public bool IsSelected { get => (bool)GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }
    public bool ShowTitle { get => (bool)GetValue(ShowTitleProperty); set => SetValue(ShowTitleProperty, value); }
    public string TabIndex { get => (string)GetValue(TabIndexProperty); set => SetValue(TabIndexProperty, value); }
    public ICommand TabCommand { get => (ICommand)GetValue(TabCommandProperty); set => SetValue(TabCommandProperty, value); }
    public bool HasBadge { get => (bool)GetValue(HasBadgeProperty); set => SetValue(HasBadgeProperty, value); }

    public CustomBottomBarItem()
    {
        InitializeComponent();
    }

    private static void OnBadgeCountChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (CustomBottomBarItem)bindable;
        var countStr = newValue as string;
        control.HasBadge = !string.IsNullOrEmpty(countStr) && countStr != "0";
    }
}