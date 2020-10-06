using System.Threading.Tasks;

namespace SogigiMind.DataAccess
{
    public interface IRemoteImageDao
    {
        Task UpdateAsync(string url, bool markAsKnown, bool? isSensitive, bool? isPublic);
    }
}
