using System.Threading.Tasks;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class BinaryMerkleTreeManager : IBinaryMerkleTreeManager
    {
        private readonly IDataStore _dataStore;

        public BinaryMerkleTreeManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        public async Task AddBinaryMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForGettingMerkleTreeByHeight(chainId, height);
            await _dataStore.InsertAsync(key, binaryMerkleTree);
        }

        public async Task<BinaryMerkleTree> GetBinaryMerkleTreeByHeightAsync(Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForGettingMerkleTreeByHeight(chainId, height);
            return await _dataStore.GetAsync<BinaryMerkleTree>(key);
        }
    }
}