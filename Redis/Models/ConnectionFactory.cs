using StackExchange.Redis;
using System;

namespace Redis
{
    public class ConnectionFactory
    {
        private Lazy<ConnectionMultiplexer> _redisCache;
        private Lazy<ConnectionMultiplexer> _redisOrderbookCache;
        public ConnectionFactory(string connectionString)
        {
            _redisCache = new Lazy<ConnectionMultiplexer>(() =>
                    ConnectionMultiplexer.Connect(connectionString));

            _redisOrderbookCache = new Lazy<ConnectionMultiplexer>(() =>
                    ConnectionMultiplexer.Connect(connectionString));
        }

        public ConnectionMultiplexer GetRedisCache()
        {
            return _redisCache.Value;
        }

        public ConnectionMultiplexer GetRedisOrderbookCache()
        {
            return _redisOrderbookCache.Value;
        }
    }
}
