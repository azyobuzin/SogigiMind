using System.Threading.Tasks;

namespace SogigiMind.Infrastructures
{
    public static class TaskExtensions
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
    }
}
