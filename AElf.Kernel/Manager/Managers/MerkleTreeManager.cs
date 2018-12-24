using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Manager.Interfaces;
using AElf.Kernel.Storage;

namespace AElf.Kernel.Manager.Managers
{
    public class MerkleTreeManager : IMerkleTreeManager
    {
        private readonly IKeyValueStore _merkleTreeStore;

        public MerkleTreeManager(MerkleTreeStore merkleTreeStore)
        {
            _merkleTreeStore = merkleTreeStore;
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

        private string GetTransactionsMerkleTreeKey(Hash chainId, ulong height)
        {
            return chainId.ToHex() + height;
        }
    }
}