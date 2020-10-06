using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SogigiMind.DataAccess;

namespace SogigiMind.Services
{
    /// <remarks>
    /// このサービスでは、サムネイルがすでに作成済みかは考慮せず、作成を行います。
    /// 作成済みかのチェックは通常 <see cref="BackgroundServices.ThumbnailBackgroundService"/> で行われます。
    /// </remarks>
    public interface IThumbnailCreationService
    {
        Task<IReadOnlyList<ThumbnailInfo>> CreateThumbnailAsync(string url, CancellationToken cancellationToken = default);
    }
}
