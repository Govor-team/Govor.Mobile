using Govor.Mobile.Application.Services.Interfaces.Wrappers;
using System.Threading.Tasks;

namespace Govor.Mobile.Application.Services.Implementations.Wrappers
{
    public class SecureStorageWrapper : ISecureStorage
    {
        public Task<string> GetAsync(string key) => SecureStorage.GetAsync(key);
        public Task SetAsync(string key, string value) => SecureStorage.SetAsync(key, value);
        public bool Remove(string key) => SecureStorage.Remove(key);
        public void RemoveAll() => SecureStorage.RemoveAll();
    }
}
