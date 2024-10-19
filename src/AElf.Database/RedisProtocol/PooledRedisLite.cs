using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using AElf.ExceptionHandler;

namespace AElf.Database.RedisProtocol;

/**
    * Simplified NServiceKit.Redis
    */
public partial class PooledRedisLite
{
    private readonly RedisLite[] _readOnlyClients;
    private readonly RedisLite[] _writeClients;
    private int _readOnlyClientIndex;

    private int _writeClientIndex;

    public PooledRedisLite(string host, int port = 6379, string password = null, int db = 0, int poolSize = 20)
    {
        Host = host ?? throw new ArgumentNullException(nameof(host));
        Port = port;
        PoolSize = poolSize;
        Db = db;
        Password = password;

        // Init clients
        _writeClients = new RedisLite[PoolSize];
        _readOnlyClients = new RedisLite[PoolSize];
        for (var i = 0; i < poolSize; i++)
        {
            _writeClients[i] = new RedisLite(host, port, Password, db);
            _readOnlyClients[i] = new RedisLite(host, port, Password, db);
        }
    }

    private int PoolSize { get; }
    private int Db { get; }
    public string Host { get; }
    public int Port { get; }
    private string Password { get; }
    public int? PoolTimeout { get; set; }
    public int RecheckPoolAfterMs { get; } = 10;

    public bool Exists(string key)
    {
        using (var client = GetRedisClient())
        {
            return client.Exists(key) > 0;
        }
    }

    public bool Ping()
    {
        using (var client = GetRedisClient(true))
        {
            return client.Ping();
        }
    }

    public void Set(string key, string value)
    {
        using (var client = GetRedisClient())
        {
            client.Set(key, Encoding.UTF8.GetBytes(value));
        }
    }

    public bool Set(string key, byte[] value)
    {
        using (var client = GetRedisClient())
        {
            client.Set(key, value);
            return true;
        }
    }

    public void SetAll(IDictionary<string, byte[]> dict)
    {
        using (var client = GetRedisClient())
        {
            client.MSet(dict.Keys.ToArray().ToMultiByteArray(), dict.Values.ToArray());
        }
    }

    public bool Remove(string key)
    {
        using (var client = GetRedisClient())
        {
            var success = client.Del(key);
            return success == RedisLite.Success;
        }
    }

    public bool RemoveAll(string[] key)
    {
        using (var client = GetRedisClient())
        {
            var success = client.Del(key);
            return success == RedisLite.Success;
        }
    }

    public byte[] Get(string key)
    {
        using (var client = GetRedisClient(true))
        {
            return client.Get(key);
        }
    }

    public string GetString(string key)
    {
        using (var client = GetRedisClient(true))
        {
            return client.Get(key).FromUtf8Bytes();
        }
    }

    public byte[][] GetAll(string[] keys)
    {
        using (var client = GetRedisClient())
        {
            return client.MGet(keys.ToMultiByteArray());
        }
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(PooledRedisLite),
        MethodName = nameof(HandleExceptionWhileGettingNodes))]
    private RedisLite GetRedisClient(bool readOnly = false)
    {
        var lockObject = readOnly ? _readOnlyClients : _writeClients;
        lock (lockObject)
        {
            RedisLite inActiveClient;
            while ((inActiveClient = GetInActiveRedisClient(readOnly)) == null)
                if (PoolTimeout.HasValue)
                {
                    // wait for a connection, cry out if made to wait too long
                    if (!Monitor.Wait(lockObject, PoolTimeout.Value))
                        throw new TimeoutException("Pool timeout error.");
                }
                else
                {
                    Monitor.Wait(lockObject, RecheckPoolAfterMs);
                }

            inActiveClient.Active = true;
            return inActiveClient;
        }
    }

    private RedisLite GetInActiveRedisClient(bool readOnly = false)
    {
        var desiredIndex = (readOnly ? _readOnlyClientIndex : _writeClientIndex) % PoolSize;
        var clients = readOnly ? _readOnlyClients : _writeClients;

        for (var i = desiredIndex; i < desiredIndex + clients.Length; i++)
        {
            var index = i % PoolSize;

            if (readOnly)
                _readOnlyClientIndex = index + 1;
            else
                _writeClientIndex = index + 1;

            if (clients[index] != null && !clients[index].Active && !clients[index].HadExceptions)
                return clients[index];

            if (clients[index] == null || clients[index].HadExceptions)
            {
                if (clients[index] != null)
                    clients[index].Dispose();
                var client = new RedisLite(Host, Port, Password, Db);

                clients[index] = client;
                return client;
            }
        }

        return null;
    }
}