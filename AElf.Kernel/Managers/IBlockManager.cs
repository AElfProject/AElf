using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IBlockManager
    {
        Task<Block> AddBlockAsync(Block block);
        Task<IBlockHeader> GetBlockHeaderAsync(Hash chainGenesisBlockHash);

    }
}