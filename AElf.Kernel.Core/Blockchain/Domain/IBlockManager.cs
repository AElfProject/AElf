using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Blockchain.Domain
{
    public interface IBlockManager
    {
        Task AddBlockHeaderAsync(BlockHeader header);
        Task AddBlockBodyAsync(Hash blockHash, BlockBody blockBody);
        Task<Block> GetBlockAsync(Hash blockHash);
        Task<BlockHeader> GetBlockHeaderAsync(Hash blockHash);
    }
}