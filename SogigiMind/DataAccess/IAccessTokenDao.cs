using System.Security.Claims;
using System.Threading.Tasks;

namespace SogigiMind.DataAccess
{
    public interface IAccessTokenDao
    {
        Task<ClaimsIdentity?> GetIdentityByTokenAsync(string token);

        Task InsertIdenityAsync(string token, ClaimsIdentity identity);
    }
}
