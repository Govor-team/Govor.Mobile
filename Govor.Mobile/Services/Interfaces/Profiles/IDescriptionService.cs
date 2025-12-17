namespace Govor.Mobile.Services.Interfaces.Profiles;

public interface IDescriptionService
{
    /// <summary>
    /// Обновляет описание пользователя через хаб.
    /// </summary>
    /// <param name="description">Новое описание.</param>
    /// <returns>True, если обновление прошло успешно.</returns>
    Task<bool> UpdateDescriptionAsync(string description);
}