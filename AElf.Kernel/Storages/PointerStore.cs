using System.IO;
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
            var changeByte = await _keyValueDatabase.GetAsync(pathHash.Value.ToBase64(), typeof(Change));
            if (changeByte == null)
                return;
            
            var change = Change.Parser.ParseFrom(changeByte);
            change.UpdateHashAfter(pointerHash);
            await _keyValueDatabase.SetAsync(pathHash.Value.ToBase64(), change.ToByteArray());
        }

        public async Task<Hash> GetAsync(Hash pathHash)
        {
            var changeByte = await _keyValueDatabase.GetAsync(pathHash.Value.ToBase64(), typeof(Change));
            var change = Change.Parser.ParseFrom(changeByte);
            return change.After;
        }
    }
}