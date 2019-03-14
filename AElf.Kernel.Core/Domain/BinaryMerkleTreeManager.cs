using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.Domain
{
    public class BinaryMerkleTreeManager : IBinaryMerkleTreeManager
    {
        private readonly IBlockchainStore<BinaryMerkleTree> _binaryMerkleTreeStore;

        public BinaryMerkleTreeManager(IBlockchainStore<BinaryMerkleTree> binaryMerkleTreeStore)
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
        public async Task AddTransactionsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, ulong height) //TODO: Add AddTransactionsMerkleTreeAsync test case [Case]
        {
            var key = GetTransactionsMerkleTreeKey(height);
            await _binaryMerkleTreeStore.SetAsync(key, binaryMerkleTree);
        }

        /// <summary> 
        /// Get <see cref="BinaryMerkleTree"/> of transactions.
        /// </summary>
        /// <param name="chainId"></param>
        /// <param name="height">=</param>
        /// <returns></returns>
        public async Task<BinaryMerkleTree> GetTransactionsMerkleTreeByHeightAsync(ulong height) //TODO: Add GetTransactionsMerkleTreeByHeightAsync test case [Case]
        {
            var key = GetTransactionsMerkleTreeKey(height);
            return await _binaryMerkleTreeStore.GetAsync(key);
        }

        private string GetTransactionsMerkleTreeKey(ulong height)
        {
            return height.ToStorageKey();
        }
    }
}