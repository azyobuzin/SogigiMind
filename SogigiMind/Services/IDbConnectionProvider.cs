using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SogigiMind.Infrastructures;
using SogigiMind.Services;

namespace SogigiMind.Services
{
    public interface IDbConnectionProvider<TConnection>
    {
        /// <summary>
        /// <paramref name="unit"/> に紐づく <typeparamref name="TConnection"/> インスタンスを返します。
        /// 戻り値は <c>Dispose</c> しないでください。
        /// </summary>
        TConnection GetConnection(UnitOfDbConnection unit);
    }

    public class DbConnectionProvider<TConnection> : IDbConnectionProvider<TConnection>
    {
        private readonly Func<TConnection> _connectionFactory;
        private readonly Dictionary<UnitOfDbConnection, TConnection> _connections = new Dictionary<UnitOfDbConnection, TConnection>();

        public DbConnectionProvider(Func<TConnection> connectionFactory)
        {
            this._connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public TConnection GetConnection(UnitOfDbConnection unit)
        {
            lock (this._connections)
            {
                if (!this._connections.TryGetValue(unit, out var connection))
                {
                    connection = this._connectionFactory();
                    this._connections.Add(unit, connection);
                    unit.RegisterDisposeAction(DisposeAction);
                }

                return connection;
            }

            ValueTask DisposeAction()
            {
                TConnection connection;
                lock (this._connections)
                {
                    if (!this._connections.Remove(unit, out connection))
                        return default;
                }

                if (connection is IAsyncDisposable ad)
                    return ad.DisposeAsync();

                if (connection is IDisposable d)
                    d.Dispose();

                return default;
            }
        }
    }
}

namespace Microsoft.Extensions.DependencyInjection
{
    public static partial class SogigiMindServiceCollectionExtensions
    {
        public static IServiceCollection AddDbConnectionProvider<TConnection>(
            this IServiceCollection services,
            Func<IServiceProvider, TConnection> connectionFactory)
        {
            return services.AddSingleton<IDbConnectionProvider<TConnection>>(serviceProvider =>
                new DbConnectionProvider<TConnection>(() => connectionFactory(serviceProvider)));
        }
    }
}
