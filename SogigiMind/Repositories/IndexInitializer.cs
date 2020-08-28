using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SogigiMind.Repositories
{
    /// <summary>
    /// 1回だけインデックス作成処理を行うヘルパークラス
    /// </summary>
    internal class IndexInitializer
    {
        private readonly Func<Task> _action;
        private readonly ILogger? _logger;
        private Task? _task;

        public IndexInitializer(Func<Task> createIndexes, ILogger? logger)
        {
            this._action = createIndexes ?? throw new ArgumentNullException(nameof(createIndexes));
            this._logger = logger;
        }

        public Task CreateIndexesAsync()
        {
            // インデックス作成処理は、複数回実行してしまっても問題ないので、雑に判定
            if (this._task is { } t)
                return t;

            t = Task.Run(async () =>
            {
                try
                {
                    await this._action().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this._logger?.LogError(ex, "Failed to initialize indexes.");
                    this._task = null; // Retry next time
                }
            });

            this._task = t;
            return t;
        }
    }
}
