using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Synchronization.BlockSynchronization
{
    public interface IBlockSet
    {
        ulong KeepHeight { get; set; }
        void AddBlock(IBlock block);
        void AddOrUpdateMinedBlock(IBlock block);
        void Tell(IBlock currentExecutedBlock);
        bool IsBlockReceived(IBlock blockHash);
        IBlock GetBlockByHash(Hash blockHash);
        IEnumerable<IBlock> GetBlocksByHeight(ulong height);
        ulong AnyLongerValidChain(ulong rollbackHeight);
        bool IsFull();
    }
}