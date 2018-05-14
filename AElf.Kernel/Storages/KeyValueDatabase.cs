using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public class KeyValueDatabase : IKeyValueDatabase
    {
        private readonly Dictionary<Hash, byte[]> _dictionary = new Dictionary<Hash, byte[]>();
        
        public Task<byte[]> GetAsync(Hash key, Type type)
        {
            return _dictionary.TryGetValue(key, out var value) ? Task.FromResult(value) : Task.FromResult<byte[]>(null);
        }

        public Task SetAsync(Hash key, byte[] bytes)
        {
            _dictionary[key] = bytes;
            return Task.CompletedTask;
        }
    }
}