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
    
    /// <summary>
    /// todo
    /// TEMP: temporary dependency of GrpcPeerService until BlockChainService gets implemented
    /// </summary>

    public class BlockService : IBlockService
    {
        public Task<Block> GetBlockAsync(Hash block)
        {
            return Task.FromResult(new Block());
        }

        public Task<Block> GetBlockByHeight(ulong height)
        {
            return Task.FromResult(new Block());
        }
    }
}