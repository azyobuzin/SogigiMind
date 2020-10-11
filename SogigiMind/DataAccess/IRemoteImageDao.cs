using System.Threading.Tasks;

namespace SogigiMind.DataAccess
{
    public interface IRemoteImageDao
    {
        Task UpdateAsync(string url, bool? isSensitive, bool? isPublic);
    }
}
