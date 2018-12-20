using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Synchronization.BlockExecution;

namespace AElf.Synchronization.BlockSynchronization
{
    public interface IBlockSynchronizer
    {
        int RollBackTimes { get; }
        
        void Init();
        
        IBlock GetBlockByHash(Hash blockHash);
        Task<BlockHeaderList> GetBlockHeaderList(ulong index, int count);
    }
}