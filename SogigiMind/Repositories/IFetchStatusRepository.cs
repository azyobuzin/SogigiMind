using System;
using System.Threading.Tasks;
using SogigiMind.Models;

namespace SogigiMind.Repositories
{
    public interface IFetchStatusRepository
    {
        Task<FetchStatus> FindByUrlAsync(string url);

        Task UpdateLeaningOptionsAsync(string url, bool? sensitive, bool? canUseToTrain);

        Task UpdateStatusAsync(string url, FetchStatusKind status, DateTimeOffset attemptedAt);

        Task UpdateThumbnailInfoAsync(
            string url, FetchStatusKind status,
            string contentType, string contentHash,
            ThumbnailInfo? thumbnailInfo, DateTimeOffset attemptedAt);
    }
}
