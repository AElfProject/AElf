using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Manager.Managers
{
    public class BinaryMerkleTreeManager : IBinaryMerkleTreeManager
    {
        private readonly IMerkleTreeStore _merkleTreeStore;

        public BinaryMerkleTreeManager(IMerkleTreeStore merkleTreeStore)
        {
            _merkleTreeStore = merkleTreeStore;
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
            await _merkleTreeStore.SetAsync(key.DumpHex(), binaryMerkleTree);
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
            await _merkleTreeStore.SetAsync(key.DumpHex(), binaryMerkleTree);
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
            return await _merkleTreeStore.GetAsync<BinaryMerkleTree>(key.DumpHex());
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
            return await _merkleTreeStore.GetAsync<BinaryMerkleTree>(key.DumpHex());
        }
    }
}