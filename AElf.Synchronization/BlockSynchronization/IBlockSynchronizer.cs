using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Synchronization.BlockExecution;

// ReSharper disable once CheckNamespace
namespace AElf.Synchronization.BlockSynchronization
{
    public interface IBlockSynchronizer
    {
        Task ReceiveBlock(IBlock block);
        IBlock GetBlockByHash(Hash blockHash);
        Task<BlockHeaderList> GetBlockHeaderList(ulong index, int count);
    }
}