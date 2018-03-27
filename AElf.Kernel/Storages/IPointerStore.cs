using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IPointerStore
    {
        Task InsertAsync(Hash path, Hash pointer);

        Task<Hash> GetAsync(Hash path);
    }
    
    public class PointerStore : IPointerStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public PointerStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }
        
        public async Task InsertAsync(Hash path, Hash pointer)
        {
            await _keyValueDatabase.SetAsync(path, pointer);
        }

        public async Task<Hash> GetAsync(Hash path)
        {
            return (Hash) await _keyValueDatabase.GetAsync(path);
        }
    }
}