using Govor.Mobile.Models;
using Govor.Mobile.Services.Interfaces;

namespace Govor.Mobile.Services.Implementations;

public class DefaultAvatarGenerator : IDefaultAvatarGenerator
{
    private static readonly Color[] DefaultColors = new[]
    {
        Color.FromHex("#FF6B6B"), Color.FromHex("#5B8C5A"), Color.FromHex("#4ECDC4"), 
        Color.FromHex("#477E90"), Color.FromHex("#EFC050")
    };
    
    public AvatarModel GenerateAvatar(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new AvatarModel("?", Colors.Gray);
        }

        var firstLetter = char.ToUpperInvariant(userName.Trim()[0]);
        var index = firstLetter % DefaultColors.Length;

        return new AvatarModel(
            Text: firstLetter.ToString(),
            BackgroundColor: DefaultColors[index]
        );
    }
}