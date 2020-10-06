using System;
using System.Threading;
using System.Threading.Tasks;

namespace SogigiMind.Infrastructures
{
    public class UnitOfDbConnection : IAsyncDisposable
    {
        private Func<ValueTask>? _disposeAction;

        public void RegisterDisposeAction(Func<ValueTask> action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (this._disposeAction != null) throw new InvalidOperationException("すでに DisposeAction が設定されています。");
            this._disposeAction = action;
        }

        public ValueTask DisposeAsync()
        {
            GC.SuppressFinalize(this);
            var action = Interlocked.Exchange(ref this._disposeAction, null);
            return action?.Invoke() ?? default;
        }

        ~UnitOfDbConnection()
        {
            var action = Interlocked.Exchange(ref this._disposeAction, null);
            action?.Invoke().AsTask();
        }
    }
}
