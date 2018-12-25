using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public interface IBlockManager
    {
        Task AddBlockAsync(IBlock block);
        Task AddBlockHeaderAsync(BlockHeader header);
        Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody);
        Task<Block> GetBlockAsync(Hash blockHash);
        Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash);
        Task<BlockBody> GetBlockBodyAsync(Hash bodyHash);
    }
}