using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.OS.Network.Temp
{
    public interface IBlockService
    {
        Task<Block> GetBlockAsync(Hash block);
        Task<Block> GetBlockByHeight(ulong height);
    }
}