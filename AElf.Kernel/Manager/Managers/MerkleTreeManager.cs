using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Storage.Interfaces;

namespace AElf.Kernel.Manager.Managers
{
    public class MerkleTreeManager : IMerkleTreeManager
    {
        private readonly IMerkleTreeStore _merkleTreeStore;

        public MerkleTreeManager(IMerkleTreeStore merkleTreeStore)
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
            var key = GetTransactionsMerkleTreeKey(chainId, height);
            await _merkleTreeStore.SetAsync(key, binaryMerkleTree);
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
            var key = GetSideChainTransactionsMerkleTreeKey(chainId, height);
            await _merkleTreeStore.SetAsync(key, binaryMerkleTree);
        }

        /// <summary> 
        /// Get <see cref="BinaryMerkleTree"/> of transactions.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height">=</param>
        /// <returns></returns>
        public async Task<BinaryMerkleTree> GetTransactionsMerkleTreeByHeightAsync(Hash chainId, ulong height)
        {
            var key = GetTransactionsMerkleTreeKey(chainId, height);
            return await _merkleTreeStore.GetAsync<BinaryMerkleTree>(key);
        }

        /// <summary> 
        /// Get <see cref="BinaryMerkleTree"/> of side chain transaction roots.
        /// </summary>
        /// <param name="chainId">Parent chain Id</param>
        /// <param name="height">Parent chain height</param>
        /// <returns></returns>
        public async Task<BinaryMerkleTree> GetSideChainTransactionRootsMerkleTreeByHeightAsync(Hash chainId, ulong height)
        {
            var key = GetSideChainTransactionsMerkleTreeKey(chainId, height);
            return await _merkleTreeStore.GetAsync<BinaryMerkleTree>(key);
        }

        private string GetTransactionsMerkleTreeKey(Hash chainId, ulong height)
        {
            return chainId.ToHex() + height;
        }
        
        private string GetSideChainTransactionsMerkleTreeKey(Hash chainId, ulong height)
        {
            return "s"+chainId.ToHex() + height;
        }
    }
}