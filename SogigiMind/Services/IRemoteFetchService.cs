using System.Net.Http;
using System.Threading.Tasks;

namespace SogigiMind.Services
{
    public interface IRemoteFetchService
    {
        Task<HttpResponseMessage> GetAsync(string requestUri);
    }
}
