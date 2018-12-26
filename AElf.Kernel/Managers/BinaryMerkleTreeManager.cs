using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Storages;

namespace AElf.Kernel.Managers
{
    public class BinaryMerkleTreeManager : IBinaryMerkleTreeManager
    {
        private readonly IBinaryMerkleTreeStore _binaryMerkleTreeStore;

        public BinaryMerkleTreeManager(IBinaryMerkleTreeStore binaryMerkleTreeStore)
        {
            _binaryMerkleTreeStore = binaryMerkleTreeStore;
        }

        // Todo change arguments height=>blockhash
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
            await _binaryMerkleTreeStore.SetAsync(key, binaryMerkleTree);
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
            return await _binaryMerkleTreeStore.GetAsync<BinaryMerkleTree>(key);
        }

        private string GetTransactionsMerkleTreeKey(Hash chainId, ulong height)
        {
            return chainId.ToHex() + height;
        }
    }
}