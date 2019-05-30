using AElf.Common;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Consensus.Application
{
    public interface IBlockTimeProvider
    {
        Timestamp GetBlockTime();
        void SetBlockTime(Timestamp blockTime);
    }

    public class BlockTimeProvider : IBlockTimeProvider
    {
        private Timestamp _blockTime;
        public Timestamp GetBlockTime()
        {
            return _blockTime == default ? TimestampHelper.GetUtcNow() : _blockTime;
        }

        public void SetBlockTime(Timestamp blockTime)
        {
            _blockTime = blockTime;
        }
    }
}