using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IBlockManagerBasic
    {
        Task<BlockHeader> AddBlockHeaderAsync(BlockHeader header);
        Task<IBlock> AddBlockAsync(IBlock block);
        Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody);
        Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash);
        Task<BlockBody> GetBlockBodyAsync(Hash bodyHash);
        Task<Block> GetBlockAsync(Hash blockHash);
    }
}