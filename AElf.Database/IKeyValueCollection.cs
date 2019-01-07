using System.Threading.Tasks;

namespace AElf.Database
{
    public interface IKeyValueCollection
    {
        string Name { get; }
        Task<byte[]> GetAsync(string key);
        Task<bool> SetAsync(string key, byte[] value);
        Task<bool> RemoveAsync(string key);

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

        public async Task<bool> SetAsync(string key, byte[] value)
        {
            return await _keyValueDatabase.SetAsync(GetKey(key), value);
        }


        protected virtual string GetKey(string key)
        {
            return Name + key;
        }
        
        public async Task<bool> RemoveAsync(string key)
        {
            return await _keyValueDatabase.RemoveAsync(GetKey(key));
        }
    }
}