using Govor.Mobile.Services.Hubs;
using Govor.Mobile.Services.Interfaces.Profiles;

namespace Govor.Mobile.Services.Implementations.Profiles;

public class DescriptionService : IDescriptionService
{
    private readonly int MaxDescriptionLength;
    private readonly IProfileHubService _profileHub;

    public DescriptionService(IProfileHubService _rofileHub, IMaxDescriptionLengthProvider provider)
    {
        _profileHub = _rofileHub;
        MaxDescriptionLength = provider.MaxDescriptionLength;
    }

    public async Task<bool> UpdateDescriptionAsync(string description)
    {
        if (description.Length > MaxDescriptionLength) 
        {
            await AppShell.DisplayException("Максимум 500 символов.");
            return false;
        }
        
        try
        {
            var result = await _profileHub.SetDescriptionAsync(description);

            if (!result.Value && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                await AppShell.DisplayException($"Ошибка: {result.ErrorMessage}");
                return false;
            }

            return true;
        }
        catch
        {
            await AppShell.DisplayException("Ошибка при отправке описания на сервер.");
            return false;
        }
    }
}