using System;
using System.Threading.Tasks;
using AElf.Node.AElfChain;

namespace AElf.Node.Protocol
{
    public interface IBlockSynchronizer
    {
        event EventHandler SyncFinished;

        // todo remove The node property : autofac circular dependency problem.
        Task Start(MainchainNodeService node, bool doInitialSync);

        void IncrementChainHeight();

        bool IsForked();
    }
}