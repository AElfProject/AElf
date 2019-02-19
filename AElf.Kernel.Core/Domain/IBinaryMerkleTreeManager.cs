using System.Threading.Tasks;

namespace AElf.Kernel.Domain
{
    public interface IBinaryMerkleTreeManager
    {
        Task AddTransactionsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, int chainId, ulong height);
        Task<BinaryMerkleTree> GetTransactionsMerkleTreeByHeightAsync(int chainId, ulong height);
    }
}