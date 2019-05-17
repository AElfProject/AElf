using System;

namespace AElf.Kernel.Consensus.Application
{
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
            return _blockTime == default ? DateTime.UtcNow : _blockTime;
        }

        public void SetBlockTime(DateTime blockTime)
        {
            _blockTime = blockTime;
        }
    }
}