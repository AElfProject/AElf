using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IBlockManagerBasic
    {
        Task AddBlockHeaderAsync(IBlockHeader header);
        Task AddBlockAsync(IBlock block);
        Task<IBlockHeader> GetBlockHeaderAsync(Hash blockId);
        Task<IBlock> GetBlockAsync(Hash blockId);
    }
}