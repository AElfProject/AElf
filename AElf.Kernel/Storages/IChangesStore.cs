using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChangesStore
    {
        Task InsertAsync(Hash path, Change before);
    }
    
    public class ChangesStore : IChangesStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public ChangesStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }
        
        public async Task InsertAsync(Hash path, Change change)
        {
            await _keyValueDatabase.SetAsync(path, change);
        }

        public async Task<Change> GetAsync(Hash path)
        {
            return (Change) await _keyValueDatabase.GetAsync(path,typeof(Change));
        }
    }

    public struct Change
    {
        public Path Before { get; set; }
        public Path After { get; set; }
    }
}