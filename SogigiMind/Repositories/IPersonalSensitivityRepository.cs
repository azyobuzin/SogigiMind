using System;
using System.Threading.Tasks;

namespace SogigiMind.Repositories
{
    public interface IPersonalSensitivityRepository
    {
        Task<bool?> GetSensitivityAsync(string user, string url);

        Task UpdateSensitivityAsync(string user, string url, bool sensitive, DateTimeOffset updatedAt);
    }
}
