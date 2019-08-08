using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public class MockRandomHashCacheService : IRandomHashCacheService
    {
        public void SetRandomHash(Hash bestChainBlockHash, Hash randomHash)
        {
        }

        public Hash GetRandomHash(Hash bestChainBlockHash)
        {
            return Hash.FromString("random");
        }

        public void SetGeneratedBlockPreviousBlockInformation(Hash blockHash, long blockHeight)
        {
        }

        public Hash GetLatestGeneratedBlockRandomHash()
        {
            return Hash.FromString("random2");
        }
    }
}