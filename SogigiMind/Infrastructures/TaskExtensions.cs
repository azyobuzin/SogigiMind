using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace SogigiMind.Infrastructures
{
    [SuppressMessage("Style", "VSTHRD200:非同期メソッドに \"Async\" サフィックスを使用する")]
    [SuppressMessage("Usage", "VSTHRD003:外部タスクを待機しない")]
    internal static class TaskExtensions
    {
        /// <summary>
        /// await されなくても <see cref="TaskScheduler.UnobservedTaskException"/> が発生しないようにします。
        /// </summary>
        public static Task<T> TouchException<T>(this Task<T> task)
        {
            var awaiter = task.ConfigureAwait(false).GetAwaiter();
            if (awaiter.IsCompleted)
                _ = task.Exception;
            else
                awaiter.OnCompleted(() => _ = task.Exception);
            return task;
        }

        public static void Catch(this Task task, Action<AggregateException> exceptionHandler)
        {
            if (task.IsCompletedSuccessfully || task.IsCanceled) return;

            _ = task.ContinueWith(
                (t, state) =>
                {
                    if (t.Exception is { } ex)
                        ((Action<AggregateException>)state!)(ex);
                },
                exceptionHandler,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
        }

        public static async Task<TResult> Select<TSource, TResult>(this Task<TSource> task, Func<TSource, TResult> selector)
        {
            return selector(await task.ConfigureAwait(false));
        }
    }
}
