using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using Microsoft.Maui.Controls.Shapes;

namespace Govor.Mobile.Pages.MainFlow.ContentViews;

public partial class BottomNavigationBar : ContentView
{
    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(nameof(SelectedIndex), typeof(int), typeof(BottomNavigationBar), 0, BindingMode.TwoWay, propertyChanged: OnSelectedIndexChanged);

    private readonly List<(Border Selection, Image Icon, Label Label)> _visualItems = new();

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public ObservableCollection<BottomNavigationItem> Items { get; } = new();

    public ICommand SelectTabCommand { get; }

    public BottomNavigationBar()
    {
        SelectTabCommand = new Command(SelectTab);
        InitializeComponent();
        Items.CollectionChanged += OnItemsChanged;
        RebuildItems();
    }

    private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((BottomNavigationBar)bindable).UpdateSelection();
    }

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RebuildItems();
    }

    private void RebuildItems()
    {
        if (TabsContainer is null)
            return;

        TabsContainer.Children.Clear();
        TabsContainer.ColumnDefinitions.Clear();
        _visualItems.Clear();

        for (var index = 0; index < Items.Count; index++)
        {
            TabsContainer.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            var item = Items[index];
            var tab = new Grid();
            Grid.SetColumn(tab, index);
            var selection = new Border
            {
                Margin = new Thickness(0, 2),
                Background = Color.FromArgb("#554A78"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(16) }
            };
            var icon = new Image
            {
                Source = item.Icon,
                HeightRequest = 27,
                WidthRequest = 27,
                Aspect = Aspect.AspectFit
            };
            var label = new Label
            {
                Text = item.Title,
                FontSize = 11,
                HorizontalOptions = LayoutOptions.Center
            };
            var content = new VerticalStackLayout
            {
                InputTransparent = true,
                Spacing = 1,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Center
            };
            content.Children.Add(icon);
            content.Children.Add(label);
            tab.Children.Add(selection);
            tab.Children.Add(content);
            tab.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = SelectTabCommand,
                CommandParameter = index
            });
            TabsContainer.Children.Add(tab);
            _visualItems.Add((selection, icon, label));
        }

        UpdateSelection();
    }

    private void UpdateSelection()
    {
        for (var index = 0; index < _visualItems.Count; index++)
        {
            var isSelected = index == SelectedIndex;
            var item = _visualItems[index];
            item.Selection.IsVisible = isSelected;
            item.Icon.Opacity = isSelected ? 1 : 0.65;
            item.Label.TextColor = isSelected ? Colors.White : Color.FromArgb("#A0A0A0");
            item.Label.FontAttributes = isSelected ? FontAttributes.Bold : FontAttributes.None;
        }
    }

    private void SelectTab(object? parameter)
    {
        if (!int.TryParse(parameter?.ToString(), out var index) ||
            index < 0 || index >= Items.Count || index == SelectedIndex)
            return;

        SelectedIndex = index;
    }
}
