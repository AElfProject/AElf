using System.Threading.Tasks;
using AElf.Kernel.Storages;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public class BinaryMerkleTreeManager : IBinaryMerkleTreeManager
    {
        private readonly IDataStore _dataStore;

        public BinaryMerkleTreeManager(IDataStore dataStore)
        {
            _dataStore = dataStore;
        }

        /// <summary>
        /// Store <see cref="BinaryMerkleTree"/> for transactions.
        /// </summary>
        /// <param name="binaryMerkleTree"></param>
        /// <param name="chainId"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task AddTransactionsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForTransactionsMerkleTreeByHeight(chainId, height);
            await _dataStore.InsertAsync(key, binaryMerkleTree);
        }


        /// <summary> 
        /// Get <see cref="BinaryMerkleTree"/> of transactions.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height">=</param>
        /// <returns></returns>
        public async Task<BinaryMerkleTree> GetTransactionsMerkleTreeByHeightAsync(Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForTransactionsMerkleTreeByHeight(chainId, height);
            return await _dataStore.GetAsync<BinaryMerkleTree>(key);
        }

    }
}