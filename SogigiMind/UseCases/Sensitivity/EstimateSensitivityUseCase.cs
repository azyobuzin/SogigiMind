using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SogigiMind.Data;
using SogigiMind.DataAccess;
using SogigiMind.Infrastructures;
using SogigiMind.Logics;

namespace SogigiMind.UseCases.Sensitivity
{
    public class EstimateSensitivityUseCase
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IRemoteImageDao _remoteImageDao;

        public EstimateSensitivityUseCase(ApplicationDbContext dbContext, IRemoteImageDao remoteImageDao)
        {
            this._dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            this._remoteImageDao = remoteImageDao ?? throw new ArgumentNullException(nameof(remoteImageDao));
        }

        public async Task<IReadOnlyList<EstimateSensitivityOutputItem>> ExecuteAsync(string acct, IReadOnlyList<EstimateSensitivityInputItem>? inputs)
        {
            if (inputs == null || inputs.Count == 0) return Array.Empty<EstimateSensitivityOutputItem>();

            var urls = inputs.Select((item, index) => (item, index))
                .ToLookup(x => UrlNormalizer.NormalizeUrl(x.item.Url));

            // RemoteImage の更新
            foreach (var g in urls)
            {
                var url = g.Key;
                var item = g.First().item;
                await this._remoteImageDao.UpdateAsync(url, false, item.IsSensitive, item.IsPublic).ConfigureAwait(false);
            }

            var results = new EstimateSensitivityOutputItem[inputs.Count];

            foreach (var g in urls)
            {
                var url = g.Key;

                var isSensitive = await this._dbContext.PersonalSensitivities
                    .Where(x => x.User.Acct == acct && x.RemoteImage.Url == url)
                    .Select(x => (bool?)x.IsSensitive)
                    .SingleOrDefaultAsync()
                    .ConfigureAwait(false);

                // TODO: 実際に推論する

                foreach (var (_, i) in g)
                    results[i] = new EstimateSensitivityOutputItem(url, null, isSensitive);
            }

            return results;
        }
    }

    public class EstimateSensitivityInputItem
    {
        public string Url { get; }

        /// <summary>
        /// Sensitivity specified by the sender.
        /// </summary>
        public bool? IsSensitive { get; }

        /// <summary>
        /// Whether the URL is attached to a public post.
        /// </summary>
        public bool? IsPublic { get; }

        public EstimateSensitivityInputItem(string url, bool? isSensitive, bool? isPublic)
        {
            this.Url = url;
            this.IsSensitive = isSensitive;
            this.IsPublic = isPublic;
        }
    }

    public class EstimateSensitivityOutputItem
    {
        public string Url { get; }

        /// <summary>
        /// 機械学習によって推論されたセンシティビティ 0～1。ただし推論に失敗した場合は <see langword="null"/>。
        /// </summary>
        public float? EstimatedSensitivity { get; }

        /// <summary>
        /// ユーザーが設定したセンシティビティ。ユーザーが設定していない場合は <see langword="null"/>。
        /// </summary>
        public bool? PersonalSensitivity { get; }

        public EstimateSensitivityOutputItem(string url, float? estimatedSensitivity, bool? personalSensitivity)
        {
            this.Url = url;
            this.EstimatedSensitivity = estimatedSensitivity;
            this.PersonalSensitivity = personalSensitivity;
        }
    }
}
