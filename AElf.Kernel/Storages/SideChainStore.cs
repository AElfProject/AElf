using System.Threading.Tasks;
using AElf.Database;
using Google.Protobuf;

namespace AElf.Kernel.Storages
{
    public class SideChainStore : ISideChainStore
    {
        private readonly IKeyValueDatabase _keyValueDatabase;

        public SideChainStore(IKeyValueDatabase keyValueDatabase)
        {
            _keyValueDatabase = keyValueDatabase;
        }

        private static uint TypeIndex => (uint) Types.SideChain;
        
        public async Task<SideChain> GetAsync(Hash chainId)
        {
            var key = chainId.GetKeyString(TypeIndex);    
            var scBytes = await _keyValueDatabase.GetAsync(key, typeof(SideChain));
            return scBytes == null ? null : SideChain.Parser.ParseFrom(scBytes);
        }

        public async Task InsertAsync(SideChain sideChain)
        {
            var key = sideChain.ChainId.GetKeyString(TypeIndex);           
            await _keyValueDatabase.SetAsync(key, sideChain.ToByteArray());
        }
    }
}