using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IBinaryMerkleTreeManager
    {
        Task AddTransactionsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, Hash chainId, ulong height);
        Task AddSideChainTransactionRootsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, Hash chainId, ulong height);
        Task<BinaryMerkleTree> GetTransactionsMerkleTreeByHeightAsync(Hash chainId, ulong height);
        Task<BinaryMerkleTree> GetSideChainTransactionRootsMerkleTreeByHeightAsync(Hash chainId, ulong height);
        Task AddIndexedTxRootMerklePathInParentChain(MerklePath path, Hash chainId, ulong height);
        Task<MerklePath> GetIndexedTxRootMerklePathInParentChain(Hash chainId, ulong height);
    }
}