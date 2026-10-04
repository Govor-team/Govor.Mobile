using Govor.Mobile.Models.Groups;
using Govor.Mobile.Services.Api.Base;
using Govor.Mobile.Services.Interfaces.JwtServices;

namespace Govor.Mobile.Services.Implementations;

public sealed class GroupMediaService(IApiClient api, IJwtProviderService session)
{
    private readonly SemaphoreSlim _downloads = new(1, 1);

    // Call only with a freshly authorized profile/mine response, never a cached membership.
    public async Task<ImageSource?> LoadAsync(GroupProfile group)
    {
        if (group.ImageId is not Guid id || session.CurrentUserId is not Guid user) return null;
        var folder = Path.Combine(FileSystem.AppDataDirectory, $"{user:N}-group-avatars");
        var path = Path.Combine(folder, $"{id:N}.png");
        await _downloads.WaitAsync();
        try
        {
            if (File.Exists(path)) return ImageSource.FromFile(path);
            var response = await api.GetFileStreamAsync($"/api/media/download/{id}");
            if (!response.IsSuccess || response.Value == null) return null;
            await using var source = response.Value.Stream;
            Directory.CreateDirectory(folder);
            var temporary = path + ".download";
            try
            {
                await using (var file = File.Create(temporary)) await source.CopyToAsync(file);
                File.Move(temporary, path, true);
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
            return session.CurrentUserId == user ? ImageSource.FromFile(path) : null;
        }
        finally { _downloads.Release(); }
    }
}
