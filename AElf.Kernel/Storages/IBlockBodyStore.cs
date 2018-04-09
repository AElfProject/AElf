using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IBlockBodyStore
    {
        Task InsertAsync(Hash txsMerkleTreeRoot, BlockBody body);
        Task<BlockBody> GetAsync(Hash blockHash);
    }
}