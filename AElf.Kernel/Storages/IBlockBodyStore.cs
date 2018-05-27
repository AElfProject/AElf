using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IBlockBodyStore
    {
        Task InsertAsync(Hash txsMerkleTreeRoot, IBlockBody body);
        Task<BlockBody> GetAsync(Hash blockHash);
    }
}