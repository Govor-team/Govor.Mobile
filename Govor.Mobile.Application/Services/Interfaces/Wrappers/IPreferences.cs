namespace Govor.Mobile.Application.Services.Interfaces.Wrappers
{
    public interface IPreferences
    {
        bool ContainsKey(string key);
        string Get(string key, string defaultValue);
        void Set(string key, string value);
        void Remove(string key);
        void Clear();
    }
}
