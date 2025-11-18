using System.Threading.Tasks;

namespace Govor.Mobile.Application.Services.Interfaces.Wrappers
{
    public interface ISecureStorage
    {
        Task<string> GetAsync(string key);
        Task SetAsync(string key, string value);
        bool Remove(string key);
        void RemoveAll();
    }
}
