using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IBlockManagerBasic
    {
        Task AddBlockHeaderAsync(IBlockHeader header);
        Task AddBlockBodyAsync(Hash blockHash, IBlockBody blockBody);
        Task<IBlockHeader> GetBlockHeaderAsync(Hash blockId);
        Task<IBlock> GetBlockAsync(Hash blockId);
    }
}