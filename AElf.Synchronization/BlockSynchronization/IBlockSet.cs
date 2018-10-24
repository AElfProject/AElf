using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.Synchronization.BlockSynchronization
{
    public interface IBlockSet
    {
        void AddBlock(IBlock block);
        void RemoveExecutedBlock(string blockHashHex);
        void Tell(IBlock currentExecutedBlock);
        bool IsBlockReceived(Hash blockHash, ulong height);
        IBlock GetBlockByHash(Hash blockHash);
        List<IBlock> GetBlockByHeight(ulong height);
        ulong AnyLongerValidChain(ulong currentHeight);
    }
}