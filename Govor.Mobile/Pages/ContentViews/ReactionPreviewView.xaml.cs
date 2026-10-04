namespace Govor.Mobile.Pages.ContentViews;

public sealed record ReactionPreviewModel(string Name, string Text, ImageSource? Image)
{
    public bool HasImage => Image != null;
}

public partial class ReactionPreviewView : ContentView
{
    public ReactionPreviewView(ReactionPreviewModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}
