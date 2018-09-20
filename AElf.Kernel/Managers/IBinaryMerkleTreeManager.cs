using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IBinaryMerkleTreeManager
    {
        Task AddBinaryMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, Hash chainId, ulong height);
        Task<BinaryMerkleTree> GetBinaryMerkleTreeByHeightAsync(Hash chainId, ulong height);
    }
}