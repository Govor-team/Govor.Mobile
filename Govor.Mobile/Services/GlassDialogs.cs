using Sharpnado.MaterialFrame;

namespace Govor.Mobile.Services;

/// <summary>Application dialogs stay on the current page, over its themed background.</summary>
public static class GlassDialogs
{
    public static Task DisplayAlertAsync(string title, string message, string accept) => AlertAsync(title, message, accept);
    public static Task<bool> DisplayAlertAsync(string title, string message, string accept, string cancel) => ConfirmAsync(title, message, accept, cancel);
    private static readonly Dictionary<ContentPage, Action> Open = new();
    private static readonly SemaphoreSlim Queue = new(1, 1);

    public static bool Dismiss(ContentPage page)
    {
        if (!Open.TryGetValue(page, out var close)) return false;
        close();
        return true;
    }

    public static async Task AlertAsync(string title, string message, string accept) =>
        _ = await ShowAsync(title, message, accept, null);

    public static async Task<bool> ConfirmAsync(string title, string message, string accept, string cancel) =>
        await ShowAsync(title, message, accept, cancel) is not null;

    public static Task<string?> PromptAsync(string title, string message, string accept = "Продолжить",
        string cancel = "Отмена", int maxLength = 256, bool password = false, bool confirm = false) =>
        ShowAsync(title, message, accept, cancel, maxLength, password, confirm);

    public static Task<string?> AttachmentSourceAsync() => ShowAsync("Прикрепить", "Фото и видео можно выбрать вместе.",
        "", "Отмена", choices: [("gallery", "Галерея · фото и видео"), ("files", "Файлы · документы и другое")]);

    private static async Task<string?> ShowAsync(string title, string message, string accept, string? cancel,
        int? maxLength = null, bool password = false, bool confirm = false,
        IReadOnlyList<(string Id, string Title)>? choices = null)
    {
        await Queue.WaitAsync();
        try { return await ShowCoreAsync(title, message, accept, cancel, maxLength, password, confirm, choices); }
        finally { Queue.Release(); }
    }

    private static Task<string?> ShowCoreAsync(string title, string message, string accept, string? cancel,
        int? maxLength = null, bool password = false, bool confirm = false,
        IReadOnlyList<(string Id, string Title)>? choices = null) => MainThread.InvokeOnMainThreadAsync(async () =>
    {
        var shell = Shell.Current;
        var current = shell?.Navigation.ModalStack.LastOrDefault() ?? shell?.CurrentPage
            ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        if (current is not ContentPage page) return null;
        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var host = page.Content as Grid;
        if (host is null)
        {
            var content = page.Content;
            page.Content = null;
            host = new Grid();
            if (content is not null) host.Add(content);
            page.Content = host;
        }
        var overlay = new Grid { BackgroundColor = Color.FromArgb("#700A0713"), ZIndex = 10000, SafeAreaEdges = SafeAreaEdges.All };
        Grid.SetRowSpan(overlay, Math.Max(1, host.RowDefinitions.Count));
        Grid.SetColumnSpan(overlay, Math.Max(1, host.ColumnDefinitions.Count));
        var stack = new VerticalStackLayout { Spacing = 14, Padding = 20 };
        stack.Add(new Label { Text = title, TextColor = Colors.White, FontSize = 21, FontAttributes = FontAttributes.Bold });
        stack.Add(new Label { Text = message, TextColor = Color.FromArgb("#E4DDEB"), FontSize = 14 });
        Entry Input(string placeholder) => new() { Placeholder = placeholder, IsPassword = password,
            MaxLength = maxLength ?? 256, TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#CBC5D8"),
            BackgroundColor = Colors.Transparent, IsTextPredictionEnabled = false, IsSpellCheckEnabled = false };
        var input = Input(password ? "Пароль" : "Резервный код");
        var repeated = Input("Повторите пароль");
        if (maxLength.HasValue) stack.Add(input);
        if (confirm) stack.Add(repeated);
        var error = new Label { TextColor = Color.FromArgb("#FFB2BE"), FontSize = 13, IsVisible = false };
        stack.Add(error);
        Action fitCard = () => { };
        void Finish(string? value)
        {
            if (completion.Task.IsCompleted) return;
            input.Unfocus(); repeated.Unfocus();
            input.Text = string.Empty; repeated.Text = string.Empty;
            host.Children.Remove(overlay);
            Open.Remove(page);
            page.Disappearing -= Disappearing;
            completion.TrySetResult(value);
        }
        void Disappearing(object? sender, EventArgs args) => Finish(null);
        var ok = new Button { Text = accept, Style = (Style)Application.Current!.Resources["ReactionActionButton"],
            BackgroundColor = Color.FromArgb("#606E58B0") };
        ok.Clicked += (_, _) =>
        {
            var value = input.Text ?? string.Empty;
            if (maxLength.HasValue && (string.IsNullOrWhiteSpace(value) || (password && value.Length < 8) || (confirm && value != repeated.Text)))
            {
                error.Text = password ? "Введите минимум 8 символов. Повтор пароля должен совпадать." : "Введите резервный код.";
                error.IsVisible = true;
                fitCard();
                return;
            }
            Finish(maxLength.HasValue ? value : string.Empty);
        };
        if (choices is null) stack.Add(ok);
        else foreach (var choice in choices)
        {
            var button = new Button { Text = choice.Title, Style = (Style)Application.Current!.Resources["ReactionActionButton"] };
            button.Clicked += (_, _) => Finish(choice.Id);
            stack.Add(button);
        }
        if (cancel is not null)
        {
            var back = new Button { Text = cancel, Style = (Style)Application.Current!.Resources["ReactionActionButton"] };
            back.Clicked += (_, _) => Finish(null);
            stack.Add(back);
        }
        var card = new Border { Style = (Style)Application.Current!.Resources["ReactionGlassBorder"],
            Margin = 20, MaximumWidthRequest = 420, VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Fill,
            Content = new MaterialFrame { Style = (Style)Application.Current.Resources["ReactionGlassFrame"], Padding = 0,
                Content = new ScrollView { Content = stack, VerticalScrollBarVisibility = ScrollBarVisibility.Never } } };
        overlay.Add(card);
        // A ScrollView inside MaterialFrame otherwise requests the whole available height.
        // Measure its content explicitly so short notices remain compact.
        fitCard = () =>
        {
            if (overlay.Width <= 40 || overlay.Height <= 40) return;
            var width = Math.Min(420, overlay.Width - 40);
            var desired = ((Microsoft.Maui.IView)stack).Measure(width, double.PositiveInfinity).Height;
            var height = Math.Min(desired, overlay.Height - 40);
            if (Math.Abs(card.HeightRequest - height) > 0.5) card.HeightRequest = height;
        };
        overlay.SizeChanged += (_, _) => fitCard();
        stack.SizeChanged += (_, _) => fitCard();
        Open[page] = () => Finish(null);
        page.Disappearing += Disappearing;
        host.Add(overlay);
        return await completion.Task;
    });
}
