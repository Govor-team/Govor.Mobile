using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Govor.Mobile.Pages.MainFlow.ContentViews;

public partial class ContentViewSwitcher : ContentView
{
    public static readonly BindableProperty SelectedIndexProperty =
        BindableProperty.Create(
            nameof(SelectedIndex),
            typeof(int),
            typeof(ContentViewSwitcher),
            0,
            BindingMode.TwoWay,
            propertyChanged: OnSelectedIndexChanged);

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public ObservableCollection<View> Views { get; } = new();

    public ContentViewSwitcher()
    {
        InitializeComponent();
        Views.CollectionChanged += OnViewsChanged;
        RebuildViews();
        UpdateVisibility();
    }

    private static void OnSelectedIndexChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((ContentViewSwitcher)bindable).UpdateVisibility();
    }

    private void OnViewsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                AddViews(e.NewStartingIndex, e.NewItems);
                break;

            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                RemoveViews(e.OldStartingIndex, e.OldItems.Count);
                break;

            case NotifyCollectionChangedAction.Replace when e.NewItems is not null:
                ReplaceViews(e.NewStartingIndex, e.NewItems);
                break;

            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
                RebuildViews();
                break;
        }

        UpdateVisibility();
    }

    private void AddViews(int index, IList newItems)
    {
        for (var offset = 0; offset < newItems.Count; offset++)
        {
            var host = CreateHost((View)newItems[offset]!);
            ViewContainer.Children.Insert(index + offset, host);
        }
    }

    private void RemoveViews(int index, int count)
    {
        for (var offset = count - 1; offset >= 0; offset--)
        {
            if (index + offset >= ViewContainer.Children.Count)
                continue;

            if (ViewContainer.Children[index + offset] is ContentView host)
                host.Content = null;

            ViewContainer.Children.RemoveAt(index + offset);
        }
    }

    private void ReplaceViews(int index, IList newItems)
    {
        for (var offset = 0; offset < newItems.Count; offset++)
        {
            if (index + offset >= ViewContainer.Children.Count)
                continue;

            if (ViewContainer.Children[index + offset] is ContentView host)
            {
                host.Content = null;
                host.Content = (View)newItems[offset]!;
            }
        }
    }

    private void RebuildViews()
    {
        if (ViewContainer is null)
            return;

        foreach (var child in ViewContainer.Children.OfType<ContentView>())
            child.Content = null;

        ViewContainer.Children.Clear();

        foreach (var view in Views)
            ViewContainer.Children.Add(CreateHost(view));
    }

    private static ContentView CreateHost(View view)
    {
        return new ContentView { Content = view };
    }

    private void UpdateVisibility()
    {
        if (ViewContainer is null)
            return;

        for (var index = 0; index < ViewContainer.Children.Count; index++)
        {
            if (ViewContainer.Children[index] is VisualElement element)
                element.IsVisible = index == SelectedIndex;
        }
    }
}
