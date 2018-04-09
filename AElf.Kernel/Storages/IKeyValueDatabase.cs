using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Extensions;

namespace AElf.Kernel.Storages
{
    public interface IKeyValueDatabase
    {
        Task<object> GetAsync(Hash key,Type type);
        Task SetAsync(Hash key, object bytes);
    }

    public class KeyValueDatabase : IKeyValueDatabase
    {
        private readonly Dictionary<Hash, object> _dictionary = new Dictionary<Hash, object>();
        
        public Task<object> GetAsync(Hash key,Type type)
        {
            if (_dictionary.TryGetValue(key, out var value))
            {
                return Task.FromResult(value);
            }

            throw new InvalidOperationException("Cannot find related value.");
        }

        public Task SetAsync(Hash key, object bytes)
        {
            _dictionary[key] = bytes;
            return Task.CompletedTask;
        }
    }
}