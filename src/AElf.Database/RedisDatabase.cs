using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Database.RedisProtocol;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Volo.Abp;

#pragma warning disable 1998

namespace AElf.Database
{
    public class RedisDatabase<TKeyValueDbContext> : IKeyValueDatabase<TKeyValueDbContext>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
//        static Lazy<ConnectionMultiplexer> _connectionMultiplexer => 
//            new Lazy<ConnectionMultiplexer>(() =>
//            {
//                var config = new ConfigurationOptions
//                {
//                
//                    EndPoints = { { "localhost", 8888 } },
//                    DefaultDatabase = 0,
//                    CommandMap = CommandMap.Twemproxy
//                };
//                
//                return ConnectionMultiplexer.Connect(config);
//            });

//        private static ConnectionMultiplexer __connectionMultiplexer;
//        public static ConnectionMultiplexer _connectionMultiplexer
//        {
//            get { return __connectionMultiplexer;}
//            set
//            {
//                __connectionMultiplexer = value;
//            }
//        }

//        private static readonly Object _multiplexerLock = new Object();

        protected CommandMap _commandMap = CommandMap.Default;
        
        public ILogger<RedisDatabase<TKeyValueDbContext>> Logger { get; set; }
        
        private readonly IDatabaseConnectionProvider _databaseConnectionProvider;
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public RedisDatabase(KeyValueDatabaseOptions<TKeyValueDbContext> options, IDatabaseConnectionProvider databaseConnectionProvider)
        {
            Check.NotNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));
            
            _databaseConnectionProvider = databaseConnectionProvider;
            _connectionMultiplexer = _databaseConnectionProvider.Connection;

//            lock (_multiplexerLock)
//            {
//                if (_connectionMultiplexer == null)
//                {
//                    var endpoint = options.ConnectionString.ToRedisEndpoint();
//                    var config = new ConfigurationOptions
//                    {
//                
//                        EndPoints = { { endpoint.Host, 8888 } },
//                        DefaultDatabase = 0,
//                        CommandMap = CommandMap.Twemproxy
//                    };
//
//                    _connectionMultiplexer = ConnectionMultiplexer.Connect(config);
//                }
//            }
        }

        public bool IsConnected()
        {
            return _connectionMultiplexer.IsConnected;
        }

        private static int getCount = 0;
        public async Task<byte[]> GetAsync(string key)
        {
            getCount++;
            Logger.LogDebug($"type: {typeof(TKeyValueDbContext).FullName}, is-co: {IsConnected()}, count: {getCount}, GetAsync key: {key}");
            Check.NotNullOrWhiteSpace(key, nameof(key));
            
            try
            {
                return await _connectionMultiplexer.GetDatabase().StringGetAsync(key);
            }
            catch (Exception e)
            {
                Logger.LogDebug($"type: {typeof(TKeyValueDbContext).FullName}, is-co: {IsConnected()}, GetAsync key error: {key}" + e);
            }

            return null;
        }

        private static int setCount = 0;
        public async Task SetAsync(string key, byte[] bytes)
        {
            setCount++;
            Logger.LogDebug($"type: {typeof(TKeyValueDbContext).FullName}, is-co: {IsConnected()}, count: {setCount}, SetAsync key: {key}");
            Check.NotNullOrWhiteSpace(key, nameof(key));
            
            try
            {
                await _connectionMultiplexer.GetDatabase().StringSetAsync(key, bytes);
            }
            catch (Exception e)
            {
                Logger.LogDebug($"type: {typeof(TKeyValueDbContext).FullName}, is-co: {IsConnected()}, SetAsync key error: {key}" + e);
            }
        }

        public async Task RemoveAsync(string key)
        {
            Logger.LogDebug($"type: {typeof(TKeyValueDbContext).FullName}, is-co: {IsConnected()}, RemoveAsync key: {key}");
            Check.NotNullOrWhiteSpace(key, nameof(key));

            try
            {
                await _connectionMultiplexer.GetDatabase().KeyDeleteAsync(key);
            }
            catch (Exception e)
            {
                Logger.LogDebug($"type: {typeof(TKeyValueDbContext).FullName}, is-co: {IsConnected()}, RemoveAsync key error: {key}" + e);
            }
        }

        public async Task SetAllAsync(Dictionary<string, byte[]> cache)
        {
            Logger.LogDebug($"is-co: {IsConnected()}, Set all");

            try
            {
                if (cache.Count == 0)
                    return;

                var keyPairs = cache.Select(entry => new KeyValuePair<RedisKey, RedisValue>(entry.Key, entry.Value))
                    .ToArray();

                await _connectionMultiplexer.GetDatabase().StringSetAsync(keyPairs);
            }
            catch (Exception e)
            {
                Logger.LogDebug($"is-co: {IsConnected()}, Set all" + e);
            }
        }
    }
}