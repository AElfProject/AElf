using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IBlockManager
    {
        Task<Block> AddBlockAsync(Block block);
        Task<BlockHeader> GetBlockHeaderAsync(Hash chainGenesisBlockHash);

    }
}