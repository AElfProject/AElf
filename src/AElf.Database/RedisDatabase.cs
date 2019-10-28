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
        private readonly ConnectionMultiplexer _connectionMultiplexer;

        protected CommandMap _commandMap = CommandMap.Default;
        
        public ILogger<RedisDatabase<TKeyValueDbContext>> Logger { get; set; }

        public RedisDatabase(KeyValueDatabaseOptions<TKeyValueDbContext> options)
        {
            Check.NotNullOrWhiteSpace(options.ConnectionString, nameof(options.ConnectionString));

            var endpoint = options.ConnectionString.ToRedisEndpoint();
            var config = new ConfigurationOptions
            {
                
                EndPoints = { { endpoint.Host, 8888 } },
                DefaultDatabase = 0,
                CommandMap = CommandMap.Twemproxy
            };

            _connectionMultiplexer = ConnectionMultiplexer.Connect(config);

        }

        public bool IsConnected()
        {
            return _connectionMultiplexer.IsConnected;
        }

        public async Task<byte[]> GetAsync(string key)
        {
            //Logger.LogDebug($"logger GetAsync key: {key}");
            Check.NotNullOrWhiteSpace(key, nameof(key));
            
            try
            {
                return await _connectionMultiplexer.GetDatabase().StringGetAsync(key);
            }
            catch (Exception e)
            {
                Logger.LogDebug($"is-co: {IsConnected()}, GetAsync key error: {key}" + e);
            }

            return null;
        }

        public async Task SetAsync(string key, byte[] bytes)
        {
            Logger.LogDebug($"is-co: {IsConnected()}, SetAsync key: {key}");
            Check.NotNullOrWhiteSpace(key, nameof(key));
            
            try
            {
                await _connectionMultiplexer.GetDatabase().StringSetAsync(key, bytes);
            }
            catch (Exception e)
            {
                Logger.LogDebug($"is-co: {IsConnected()}, SetAsync key error: {key}" + e);
            }
        }

        public async Task RemoveAsync(string key)
        {
            Logger.LogDebug($"is-co: {IsConnected()}, RemoveAsync key: {key}");
            Check.NotNullOrWhiteSpace(key, nameof(key));

            try
            {
                await _connectionMultiplexer.GetDatabase().KeyDeleteAsync(key);
            }
            catch (Exception e)
            {
                Logger.LogDebug($"is-co: {IsConnected()}, RemoveAsync key error: {key}" + e);
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