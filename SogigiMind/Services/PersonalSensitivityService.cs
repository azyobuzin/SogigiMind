using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SogigiMind.Infrastructures;
using SogigiMind.Logics;
using SogigiMind.Repositories;

namespace SogigiMind.Services
{
    public class PersonalSensitivityService
    {
        private readonly IPersonalSensitivityRepository _personalSensitivityRepository;
        private readonly IFetchStatusRepository _fetchStatusRepository;
        private readonly ILogger _logger;

        public PersonalSensitivityService(
            IPersonalSensitivityRepository personalSensitivityRepository,
            IFetchStatusRepository fetchStatusRepository,
            ILogger<PersonalSensitivityService>? logger)
        {
            this._personalSensitivityRepository = personalSensitivityRepository ?? throw new ArgumentNullException(nameof(personalSensitivityRepository));
            this._fetchStatusRepository = fetchStatusRepository ?? throw new ArgumentNullException(nameof(fetchStatusRepository));
            this._logger = (ILogger?)logger ?? NullLogger.Instance;
        }

        public Task RecordPersonalSensitivityAsync(string user, string url, bool sensitive)
        {
            url = UrlNormalizer.NormalizeUrl(url);
            return this._personalSensitivityRepository.UpdateSensitivityAsync(user, url, sensitive, DateTimeOffset.Now);
        }

        public async Task<IReadOnlyList<PersonalSensitivityEstimationResult>> EstimatePersonalSensitivityAsync(
            string user, IEnumerable<PersonalSensitivityEstimationInput> inputs)
        {
            var urls = new List<string>();

            foreach (var input in inputs)
            {
                var url = UrlNormalizer.NormalizeUrl(input.Url);
                urls.Add(url);

                // FetchStatus の更新は裏でよろしく
                this._fetchStatusRepository
                    .UpdateLeaningOptionsAsync(url, input.SensitiveByDefault, input.CanUseToTrain)
                    .Catch(ex => this._logger.LogError(ex, "Failed to UpdateLearningOptions. ({Url})", url));
            }

            var results = new List<PersonalSensitivityEstimationResult>(urls.Count);

            foreach (var url in urls)
            {
                var sensitive = await this._personalSensitivityRepository
                    .GetSensitivityAsync(user, url)
                    .ConfigureAwait(false);

                // TODO: 実際に推論する

                results.Add(new PersonalSensitivityEstimationResult(url, null, sensitive));
            }

            Debug.Assert(results.Count == urls.Count);
            return results;
        }
    }

    public class PersonalSensitivityEstimationInput
    {
        public string Url { get; }

        /// <summary>
        /// 投稿者が設定したセンシティビティ
        /// </summary>
        public bool? SensitiveByDefault { get; }

        /// <summary>
        /// 学習データとして利用できるデータか
        /// </summary>
        public bool? CanUseToTrain { get; }

        public PersonalSensitivityEstimationInput(string url, bool? sensitiveByDefault, bool? canUseToTrain)
        {
            this.Url = url;
            this.SensitiveByDefault = sensitiveByDefault;
            this.CanUseToTrain = canUseToTrain;
        }
    }

    public class PersonalSensitivityEstimationResult
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

        public PersonalSensitivityEstimationResult(string url, float? estimatedSensitivity, bool? personalSensitivity)
        {
            this.Url = url;
            this.EstimatedSensitivity = estimatedSensitivity;
            this.PersonalSensitivity = personalSensitivity;
        }
    }
}
