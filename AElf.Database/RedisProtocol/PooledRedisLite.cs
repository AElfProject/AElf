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
        public string Password { get; set; }
        public int? PoolTimeout { get; set; }
        public int RecheckPoolAfterMs { get; } = 100;

        public PooledRedisLite(string host, int port = 6379, int db = 0, int poolSize = 5)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            PoolSize = poolSize;
            Db = db;
            _redisLites = new RedisLite[PoolSize];
        }

        public bool Ping()
        {
            var client = GetSimpleRedisLite();
            try
            {
                return client.Ping();
            }
            catch (Exception ex)
            {
                throw new RedisException("Got exception while ping.", ex);
            }
            finally
            {
                client.Active = false;
            }
        }

        public void Set(string key, string value)
        {
            var client = GetSimpleRedisLite();
            try
            {
                client.Set(key, Encoding.UTF8.GetBytes(value));
            }
            catch (Exception ex)
            {
                throw new RedisException("Got exception while set value.", ex);
            }
            finally
            {
                client.Active = false;
            }
        }

        public bool Set(string key, byte[] value)
        {
            var client = GetSimpleRedisLite();
            try
            {
                client.Set(key, value);
                return true;
            }
            catch (Exception ex)
            {
                throw new RedisException("Got exception while set value.", ex);
            }
            finally
            {
                client.Active = false;
            }
        }

        public void SetAll(IDictionary<string, byte[]> dict)
        {
            var client = GetSimpleRedisLite();
            try
            {
                client.MSet(dict.Keys.ToArray().ToMultiByteArray(), dict.Values.ToArray());
            }
            catch (Exception ex)
            {
                throw new RedisException("Got exception while set all.", ex);
            }
            finally
            {
                client.Active = false;
            }
        }

        public bool Remove(string key)
        {
            var client = GetSimpleRedisLite();
            try
            {
                var success = client.Del(key);
                return success == RedisLite.Success;
            }
            catch (Exception ex)
            {
                throw new RedisException("Got exception while remove.", ex);
            }
            finally
            {
                client.Active = false;
            }
        }

        public byte[] Get(string key)
        {
            var client = GetSimpleRedisLite();
            try
            {
                return client.Get(key);
            }
            catch (Exception ex)
            {
                throw new RedisException("Got exception while get value.", ex);
            }
            finally
            {
                client.Active = false;
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
            for (var i = 0; i < _redisLites.Length; i++)
            {
                if (_redisLites[i] != null && !_redisLites[i].Active && !_redisLites[i].HadExceptions)
                    return _redisLites[i];

                if (_redisLites[i] == null || _redisLites[i].HadExceptions)
                {
                    if (_redisLites[i] != null)
                        _redisLites[i].Dispose();
                    var client = new RedisLite(Host, Port, Password, Db);

                    _redisLites[i] = client;
                    return client;
                }
            }

            return null;
        }
    }
}