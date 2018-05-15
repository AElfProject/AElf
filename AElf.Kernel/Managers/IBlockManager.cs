using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IBlockManager
    {
        Task<IBlock> AddBlockAsync(IBlock block);
        Task<IBlockHeader> GetBlockHeaderAsync(Hash chainGenesisBlockHash);
        Task<IBlockHeader> AddBlockHeaderAsync(IBlockHeader header);
    }
}