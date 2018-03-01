using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IBlockManager
    {
        Task<IBlock> AddBlockAsync(IBlock block);
        Task<IBlockHeader> GetBlockHeaderAsync(IHash<IBlockHeader> chainGenesisBlockHash);
    }
}