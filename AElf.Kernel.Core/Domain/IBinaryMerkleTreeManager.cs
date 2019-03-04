using System.Threading.Tasks;

namespace AElf.Kernel.Domain
{
    public interface IBinaryMerkleTreeManager
    {
        Task AddTransactionsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, ulong height);
        Task<BinaryMerkleTree> GetTransactionsMerkleTreeByHeightAsync(ulong height);
    }
}