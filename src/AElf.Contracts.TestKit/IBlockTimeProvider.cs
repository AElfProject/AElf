using System;

namespace AElf.Contracts.TestKit
{
    /// <summary>
    /// Some contract method's result based on the calling time,
    /// like GetConsensusCommand method of consensus contract.
    /// </summary>
    public interface IBlockTimeProvider
    {
        DateTime GetBlockTime();
        void SetBlockTime(DateTime blockTime);
    }

    public class BlockTimeProvider : IBlockTimeProvider
    {
        private DateTime _blockTime;
        public DateTime GetBlockTime()
        {

            return _blockTime == new DateTime() ? DateTime.UtcNow : _blockTime;
        }

        public void SetBlockTime(DateTime blockTime)
        {
            _blockTime = blockTime;
        }
    }
}