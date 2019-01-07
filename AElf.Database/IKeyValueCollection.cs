using System.Threading.Tasks;

namespace AElf.Database
{
    public interface IKeyValueCollection<TValue>
    {
        string Name { get; }
        Task<TValue> GetAsync(string key);
        Task SetAsync(TValue value);
    }

    public class KeyValueCollection<TValue> : IKeyValueCollection<TValue>
    {
        private IKeyValueDatabase _keyValueDatabase;
        
        public KeyValueCollection(string name, IKeyValueDatabase keyValueDatabase)
        {
            Name = name;
            _keyValueDatabase = keyValueDatabase;
        }

        public string Name { get; }
        public Task<TValue> GetAsync(string key)
        {
            throw new System.NotImplementedException();
        }

        public Task SetAsync(TValue value)
        {
            throw new System.NotImplementedException();
        }
    }
}