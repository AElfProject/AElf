using AElf.Common;

namespace AElf.Kernel.Consensus.Application
{
    public interface IConsensusScheduler
    {
        void Launch(int countingMilliseconds, int chainId, Hash preBlockHash, ulong preBlockHeight);
        void TryToStop();
    }
}