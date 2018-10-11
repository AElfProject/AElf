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
        /// Store <see cref="BinaryMerkleTree"/> for side chain transaction roots.
        /// </summary>
        /// <param name="binaryMerkleTree"></param>
        /// <param name="chainId">Parent chain Id</param>
        /// <param name="height"></param>
        /// <returns></returns>
        public async Task AddSideChainTransactionRootsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, 
            Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForSideChainTxRootsMerkleTreeByHeight(chainId, height);
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

        /// <summary> 
        /// Get <see cref="BinaryMerkleTree"/> of side chain transaction roots.
        /// </summary>
        /// <param name="chainId">Parent chain Id</param>
        /// <param name="height">Parent chain height</param>
        /// <returns></returns>
        public async Task<BinaryMerkleTree> GetSideChainTransactionRootsMerkleTreeByHeightAsync(Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForSideChainTxRootsMerkleTreeByHeight(chainId, height);
            return await _dataStore.GetAsync<BinaryMerkleTree>(key);
        }

        /// <summary>
        /// Add <see cref="MerklePath"/> for tx root of a block indexed by parent chain.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="chainId">Child chain id</param>
        /// <param name="height">Child chain height</param>
        /// <returns></returns>
        public async Task AddIndexedTxRootMerklePathInParentChain(MerklePath path, Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForIndexedTxRootMerklePathByHeight(chainId, height);
            await _dataStore.InsertAsync(key, path);
        }
        
        /// <summary>
        /// Add <see cref="MerklePath"/> for tx root of a block indexed by parent chain.
        /// </summary>
        /// <param name="chainId">Child chain id</param>
        /// <param name="height">Child chain height</param>
        /// <returns></returns>
        public async Task<MerklePath> GetIndexedTxRootMerklePathInParentChain(Hash chainId, ulong height)
        {
            var key = DataPath.CalculatePointerForIndexedTxRootMerklePathByHeight(chainId, height);
            return await _dataStore.GetAsync<MerklePath>(key);
        }
    }
}