using System.Linq;
using System.Threading.Tasks;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public class PointerStore : IPointerStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public PointerStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        public async Task UpdateAsync(Hash pathHash, Hash pointerHash)
        {
            var bytes = pointerHash.ToByteArray();
            await _keyValueDatabase.SetAsync(pathHash, bytes);
        }

        public async Task<Hash> GetAsync(Hash pathHash)
        {
            var bytes = await _keyValueDatabase.GetAsync(pathHash.Clone(), typeof(Hash));
            var value = Hash.Parser.ParseFrom(bytes);
            return value;
        }
    }
}