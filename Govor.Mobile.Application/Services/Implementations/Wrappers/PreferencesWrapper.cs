using Govor.Mobile.Application.Services.Interfaces.Wrappers;

namespace Govor.Mobile.Application.Services.Implementations.Wrappers
{
    public class PreferencesWrapper : IPreferences
    {
        public bool ContainsKey(string key) => Preferences.ContainsKey(key);
        public string Get(string key, string defaultValue) => Preferences.Get(key, defaultValue);
        public void Set(string key, string value) => Preferences.Set(key, value);
        public void Remove(string key) => Preferences.Remove(key);
        public void Clear() => Preferences.Clear();
    }
}
