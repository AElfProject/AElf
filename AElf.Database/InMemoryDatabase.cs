using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Database
{
    public class InMemoryDatabase : IKeyValueDatabase
    {
        private readonly ConcurrentDictionary<string, byte[]> _dictionary = new ConcurrentDictionary<string, byte[]>();
        
        public Task<byte[]> GetAsync(string database, string key)
        {
            return _dictionary.TryGetValue(key, out var value) ? Task.FromResult(value) : Task.FromResult<byte[]>(null);
        }

        public Task SetAsync(string database, string key, byte[] bytes)
        {
            _dictionary[key] = bytes;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string database, string key)
        {
            _dictionary.TryRemove(key, out _);
            return Task.CompletedTask;
        }

        public async Task<bool> PipelineSetAsync(string database, Dictionary<string, byte[]> cache)
        {
            if (cache.Count == 0)
            {
                return true;
            }
            return await Task.Factory.StartNew(() =>
            {
                foreach (var change in cache)
                {
                    _dictionary[change.Key] = change.Value;
                }

                return true;
            });
        }

        public bool IsConnected(string database = "")
        {
            return true;
        }
    }
}