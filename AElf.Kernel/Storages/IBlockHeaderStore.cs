using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IBlockHeaderStore
    {
        Task InsertAsync(BlockHeader block);

        Task<BlockHeader> GetAsync(Hash blockHash);
    }
}