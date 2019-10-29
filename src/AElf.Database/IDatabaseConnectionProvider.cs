using StackExchange.Redis;

namespace AElf.Database
{
    public interface IDatabaseConnectionProvider
    {
        IConnectionMultiplexer Connection { get; }
    }

    public class RedisDatabaseConnectionProvider : IDatabaseConnectionProvider
    {
        private readonly IConnectionMultiplexer _connection;
        public IConnectionMultiplexer Connection => _connection;

        public RedisDatabaseConnectionProvider(IConnectionMultiplexer connection)
        {
            _connection = connection;
        }
    }
}