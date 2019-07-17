using System;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.TestKit
{
    /// <summary>
    /// Some contract method's result based on the calling time,
    /// like GetConsensusCommand method of consensus contract.
    /// </summary>
    public interface IBlockTimeProvider
    {
        Timestamp GetBlockTime();
        void SetBlockTime(DateTime blockTime);
        void SetBlockTime(Timestamp blockTime);
    }

    public class BlockTimeProvider : IBlockTimeProvider
    {
        private Timestamp _blockTime;
        public Timestamp GetBlockTime()
        {

            return _blockTime == null ? TimestampHelper.GetUtcNow() : _blockTime;
        }

        public void SetBlockTime(DateTime blockTime)
        {
            _blockTime = blockTime.ToTimestamp();
        }

        public void SetBlockTime(Timestamp blockTime)
        {
            _blockTime = blockTime;
        }
    }
}