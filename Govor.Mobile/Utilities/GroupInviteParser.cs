using System.Text.RegularExpressions;

namespace Govor.Mobile.Utilities;

public static class GroupInviteParser
{
    public static string Parse(string input, string apiBaseUrl)
    {
        var value = input.Trim();
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            var trusted = new Uri(apiBaseUrl);
            var isAppLink = uri.Scheme == "govor" && uri.Host == "invite";
            if (!isAppLink && (uri.Scheme != trusted.Scheme || uri.Host != trusted.Host || uri.Port != trusted.Port))
                throw new ArgumentException("Ссылка должна вести на ваш сервер Govor.");
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            value = isAppLink && parts.Length == 1 ? parts[0]
                : parts.Length == 2 && parts[0] == "invite" ? parts[1]
                : throw new ArgumentException("Ожидается ссылка /invite/код.");
        }
        if (!Regex.IsMatch(value, "^[A-Za-z0-9_-]{1,128}$"))
            throw new ArgumentException("Введите код приглашения или полную ссылку Govor.");
        return value;
    }
}
