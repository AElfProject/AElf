using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Manager.Interfaces
{
    public interface IMerkleTreeManager
    {
        Task AddTransactionsMerkleTreeAsync(BinaryMerkleTree binaryMerkleTree, Hash chainId, ulong height);
        Task<BinaryMerkleTree> GetTransactionsMerkleTreeByHeightAsync(Hash chainId, ulong height);
    }
}