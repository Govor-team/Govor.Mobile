using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Govor.Mobile.UIKit.TabView;

public partial class NasTabView : ContentView
{
    public static readonly BindableProperty TabBarHeightProperty =
        BindableProperty.Create(nameof(TabBarHeight), typeof(double), typeof(NasTabView), 40.0);

    public static readonly BindableProperty EnableSwipingProperty =
        BindableProperty.Create(nameof(EnableSwiping), typeof(bool), typeof(NasTabView), true);
    
    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(NasTabView), 0, 
            propertyChanged: OnSelectedIndexChanged);

    public double TabBarHeight
    {
        get => (double)GetValue(TabBarHeightProperty);
        set => SetValue(TabBarHeightProperty, value);
    }

    public bool EnableSwiping
    {
        get => (bool)GetValue(EnableSwipingProperty);
        set => SetValue(EnableSwipingProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }
    
    public static readonly BindableProperty TabItemsProperty =
        BindableProperty.Create(nameof(TabItems), typeof(ObservableCollection<CustomTabItem>), typeof(NasTabView), 
            defaultValueCreator: bindable => new ObservableCollection<CustomTabItem>());

    public ObservableCollection<CustomTabItem> TabItems
    {
        get => (ObservableCollection<CustomTabItem>)GetValue(TabItemsProperty);
        set => SetValue(TabItemsProperty, value);
    }

    public NasTabView()
    {
        InitializeComponent();
        
        ContentCarousel.SetBinding(ItemsView.ItemsSourceProperty, new Binding(nameof(TabItems), source: this));
        
        TabItems.CollectionChanged += (s, e) => RebuildHeaders();

        SizeChanged += OnControlSizeChanged;
    }

    private void OnItemsCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        RebuildHeaders();
    }

    // Строим Grid заголовков динамически
    private void RebuildHeaders()
    {
        HeadersGrid.Children.Clear();
        HeadersGrid.ColumnDefinitions.Clear();

        if (TabItems.Count == 0) return;

        for (int i = 0; i < TabItems.Count; i++)
        {
            HeadersGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });

            var item = TabItems[i];
            int index = i; // capture variable
            
            var label = new Label
            {
                Text = item.Header,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                TextColor = Colors.White, // Можно сделать Bindable
                FontAttributes = FontAttributes.Bold,
                FontSize = 14,
                Opacity = 0.6 // По умолчанию немного прозрачный
            };

            // 3. Контейнер для клика (GestureRecognizer)
            var container = new Grid { BackgroundColor = Colors.Transparent };
            container.Children.Add(label);
            
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => 
            {
                SelectedIndex = index; // Это триггернет смену вкладки
            };
            container.GestureRecognizers.Add(tap);

            Grid.SetColumn(container, i);
            HeadersGrid.Children.Add(container);
        }
        
        // Обновляем визуальное состояние (подсветка активного)
        UpdateVisualState();
    }

    // Обработчик изменения свойства SelectedIndex (MVVM или код)
    private static async void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var control = (NasTabView)bindable;
        var newIndex = (int)newValue;

        // 1. Скроллим контент
        if (control.ContentCarousel.Position != newIndex)
        {
            control.ContentCarousel.Position = newIndex;
        }

        // 2. Анимируем хедер
        await control.AnimateSelectionAsync(newIndex);
    }
    
    private void OnCarouselPositionChanged(object sender, PositionChangedEventArgs e)
    {
        if (SelectedIndex != e.CurrentPosition)
        {
            SelectedIndex = e.CurrentPosition;
        }
    }

    private void OnControlSizeChanged(object sender, EventArgs e)
    {
        _ = AnimateSelectionAsync(SelectedIndex, false); 
    }

    private async Task AnimateSelectionAsync(int index, bool animate = true)
    {
        if (TabItems.Count == 0 || HeadersGrid.Width <= 0) return;
        
        double tabWidth = HeadersGrid.Width / TabItems.Count;
        
        SelectionIndicator.WidthRequest = tabWidth;
        
        double targetX = index * tabWidth;

        // Анимация перемещения
        if (animate)
        {
            await SelectionIndicator.TranslateTo(targetX, 0, 250, Easing.CubicOut);
        }
        else
        {
            SelectionIndicator.TranslationX = targetX;
        }

        UpdateVisualState();
    }

    private void UpdateVisualState()
    {
        for (int i = 0; i < HeadersGrid.Children.Count; i++)
        {
            if (HeadersGrid.Children[i] is Grid container && container.Children[0] is Label lbl)
            {
                bool isSelected = i == SelectedIndex;
                lbl.FadeTo(isSelected ? 1.0 : 0.5, 200);
                lbl.ScaleTo(isSelected ? 1.05 : 1.0, 200);
            }
        }
    }
}