using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.Application
{
    public interface IBlockTimeProvider
    {
        Timestamp GetBlockTime(Hash blockHash);
        void SetBlockTime(Timestamp blockTime, Hash blockHash);
    }

    public class BlockTimeProvider : IBlockTimeProvider, ISingletonDependency
    {
        private Timestamp _blockTime;

        public Timestamp GetBlockTime(Hash blockHash)
        {
            return _blockTime == default ? TimestampHelper.GetUtcNow() : _blockTime;
        }

        public void SetBlockTime(Timestamp blockTime, Hash blockHash)
        {
            _blockTime = blockTime;
        }
    }
}