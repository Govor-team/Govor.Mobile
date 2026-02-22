using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Govor.Mobile.Pages.MainFlow.ContentViews;

public partial class FloatingBottomBarView : ContentView
{
    // Пробрасываем индекс
    public static readonly BindableProperty SelectedIndexProperty = 
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(FloatingBottomBarView), 0, BindingMode.TwoWay);

    // Пробрасываем команду
    public static readonly BindableProperty TabCommandProperty = 
        BindableProperty.Create(nameof(TabCommand), typeof(ICommand), typeof(FloatingBottomBarView));

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public ICommand TabCommand
    {
        get => (ICommand)GetValue(TabCommandProperty);
        set => SetValue(TabCommandProperty, value);
    }

    public FloatingBottomBarView()
    {
        InitializeComponent();
    }
}