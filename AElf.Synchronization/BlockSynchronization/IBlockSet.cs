using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.Synchronization.BlockSynchronization
{
    public interface IBlockSet
    {
        ulong KeepHeight { get; set; }
        int InvalidBlockCount { get; }
        void AddBlock(IBlock block);
        void AddOrUpdateBlock(IBlock block);
        void Tell(IBlock currentExecutedBlock);
        bool IsBlockReceived(Hash blockHash, ulong height);
        IBlock GetBlockByHash(Hash blockHash);
        IEnumerable<IBlock> GetBlocksByHeight(ulong height);
        ulong AnyLongerValidChain(ulong rollbackHeight);
        void InformRollback(ulong targetHeight, ulong currentHeight);
        bool IsFull();
    }
}