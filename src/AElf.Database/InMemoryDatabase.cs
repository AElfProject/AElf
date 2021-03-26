using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace AElf.Database
{
    public class InMemoryDatabase<TKeyValueDbContext> : IKeyValueDatabase<TKeyValueDbContext>
        where TKeyValueDbContext : KeyValueDbContext<TKeyValueDbContext>
    {
        private readonly ConcurrentDictionary<string, byte[]> _dictionary = new ConcurrentDictionary<string, byte[]>();

        public Task<byte[]> GetAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));

            return _dictionary.TryGetValue(key, out var value) ? Task.FromResult(value) : Task.FromResult<byte[]>(null);
        }

        public Task SetAsync(string key, byte[] bytes)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));

            _dictionary[key] = bytes;
            return Task.FromResult(true);
        }

        public Task RemoveAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));

            _dictionary.TryRemove(key, out _);
            return Task.FromResult(true);
        }

        public Task<List<byte[]>> GetAllAsync(IList<string> keys)
        {
            if (keys.Count == 0)
                return null;

            var result = new List<byte[]>();
            
            foreach (var key in keys)
            {
                Check.NotNullOrWhiteSpace(key, nameof(key));
                _dictionary.TryGetValue(key, out var value);
                result.Add(value);
            }

            return result.Any() ? Task.FromResult(result) : Task.FromResult<List<byte[]>>(null);
        }

        public Task SetAllAsync(IDictionary<string, byte[]> values)
        {
            foreach (var pair in values)
            {
                Check.NotNullOrWhiteSpace(pair.Key, nameof(pair.Key));
                _dictionary[pair.Key] = pair.Value;
            }
            return Task.FromResult(true);
        }

        public Task RemoveAllAsync(IList<string> keys)
        {
            if (keys.Count == 0)
                return Task.CompletedTask;
            
            foreach (var key in keys)
            {
                Check.NotNullOrWhiteSpace(key, nameof(key));
                _dictionary.TryRemove(key, out _);
            }
            
            return Task.CompletedTask;
        }

        public Task<bool> IsExistsAsync(string key)
        {
            Check.NotNullOrWhiteSpace(key, nameof(key));
            
            return Task.FromResult(_dictionary.ContainsKey(key));
        }

        public bool IsConnected()
        {
            return true;
        }
    }
}