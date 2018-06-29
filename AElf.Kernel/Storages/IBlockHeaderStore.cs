using System.Threading.Tasks;
using AElf.Kernel.Types;

namespace AElf.Kernel.Storages
{
    public interface IBlockHeaderStore
    {
        Task <BlockHeader> InsertAsync(BlockHeader block);

        Task<BlockHeader> GetAsync(Hash blockHash);
    }
}