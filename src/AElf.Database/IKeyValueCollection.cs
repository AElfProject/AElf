using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AElf.Database
{
    public interface IKeyValueCollection
    {
        string Name { get; }
        Task<byte[]> GetAsync(string key);
        Task SetAsync(string key, byte[] value);
        Task RemoveAsync(string key);
        
        Task SetAllAsync(IDictionary<string, byte[]> cache);

    }

    public class KeyValueCollection<TKeyValueDbContext> : IKeyValueCollection
        where TKeyValueDbContext: KeyValueDbContext<TKeyValueDbContext>
    {
        private IKeyValueDatabase<TKeyValueDbContext> _keyValueDatabase;
        
        public KeyValueCollection(string name, IKeyValueDatabase<TKeyValueDbContext> keyValueDatabase)
        {
            Name = name;
            _keyValueDatabase = keyValueDatabase;
        }

        public string Name { get; }
        public Task<byte[]> GetAsync(string key)
        {
            return _keyValueDatabase.GetAsync(GetKey(key));
        }

        public async Task SetAsync(string key, byte[] value)
        {
            await _keyValueDatabase.SetAsync(GetKey(key), value);
        }


        protected virtual string GetKey(string key)
        {
            return Name + key;
        }
        
        public async Task RemoveAsync(string key)
        {
            await _keyValueDatabase.RemoveAsync(GetKey(key));
        }

        public async Task SetAllAsync(IDictionary<string, byte[]> cache)
        {
            var dic =  cache.ToDictionary(k=> GetKey(k.Key),v => v.Value);
            await _keyValueDatabase.SetAllAsync(dic);
        }
    }
}