using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IBlockManager
    {
        Task<IBlock> AddBlockAsync(IBlock block);
        Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash);
        Task<BlockHeader> AddBlockHeaderAsync(BlockHeader header);
        Task<Block> GetBlockAsync(Hash blockHash);
    }
}