using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AElf.Database.RedisProtocol
{
    /**
    * Simplified NServiceKit.Redis
    */
    public class PooledRedisLite
    {
        private readonly RedisLite[] _redisLites;
        private int PoolSize { get; }
        private int Db { get; }
        public string Host { get; }
        public int Port { get; }
        private string Password { get; }
        public int? PoolTimeout { get; set; }
        public int RecheckPoolAfterMs { get; } = 10;

        private int _poolIndex = 0;

        public PooledRedisLite(string host, int port = 6379, string password = null, int db = 0, int poolSize = 20)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            PoolSize = poolSize;
            Db = db;
            Password = password;
            _redisLites = new RedisLite[PoolSize];
            for (var i = 0; i < poolSize; i++)
            {
                _redisLites[i] = new RedisLite(host, port, Password, db);
            }
        }
        public bool Ping()
        {
            using (var client = GetSimpleRedisLite())
            {
                return client.Ping();
            }
        }

        public void Set(string key, string value)
        {
            using (var client = GetSimpleRedisLite())
            {
                client.Set(key, Encoding.UTF8.GetBytes(value));
            }
        }

        public bool Set(string key, byte[] value)
        {
            using (var client = GetSimpleRedisLite())
            {
                client.Set(key, value);
                return true;
            }
        }

        public void SetAll(IDictionary<string, byte[]> dict)
        {
            using (var client = GetSimpleRedisLite())
            {
                client.MSet(dict.Keys.ToArray().ToMultiByteArray(), dict.Values.ToArray());
            }
        }

        public bool Remove(string key)
        {
            using (var client = GetSimpleRedisLite())
            {
                var success = client.Del(key);
                return success == RedisLite.Success;
            }
        }

        public byte[] Get(string key)
        {
            using (var client = GetSimpleRedisLite())
            {
                return client.Get(key);
            }
        }

        public string GetString(string key)
        {
            using (var client = GetSimpleRedisLite())
            {
                return client.Get(key).FromUtf8Bytes();

            }
        }

        private RedisLite GetSimpleRedisLite()
        {
            try
            {
                lock (_redisLites)
                {
                    RedisLite inActiveClient;
                    while ((inActiveClient = GetInActiveSimpleRedisLite()) == null)
                    {
                        if (PoolTimeout.HasValue)
                        {
                            // wait for a connection, cry out if made to wait too long
                            if (!Monitor.Wait(_redisLites, PoolTimeout.Value))
                                throw new TimeoutException("Pool timeout error.");
                        }
                        else
                            Monitor.Wait(_redisLites, RecheckPoolAfterMs);
                    }

                    inActiveClient.Active = true;
                    return inActiveClient;
                }
            }
            catch (Exception ex)
            {
                throw new RedisException("Got exception while get redis client.", ex);
            }
        }

        private RedisLite GetInActiveSimpleRedisLite()
        {
            var desiredIndex = (_poolIndex) % PoolSize;
            for (var i = desiredIndex; i < desiredIndex + _redisLites.Length; i++)
            {
                var index = i % PoolSize;
                _poolIndex = index + 1;

                if (_redisLites[index] != null && !_redisLites[index].Active && !_redisLites[index].HadExceptions)
                {
                    return _redisLites[index];
                }
                
                if (_redisLites[index] == null || _redisLites[index].HadExceptions)
                {
                    if (_redisLites[index] != null)
                        _redisLites[index].Dispose();
                    var client = new RedisLite(Host, Port, Password, Db);

                    _redisLites[index] = client;
                    return client;
                }
            }

            return null;
        }
    }
}