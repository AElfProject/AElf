using System;
using AElf.Sdk.CSharp;

namespace AElf.Kernel.Consensus.Application
{
    public interface IBlockTimeProvider
    {
        SafeDateTime GetBlockTime();
        void SetBlockTime(SafeDateTime blockTime);
    }

    public class BlockTimeProvider : IBlockTimeProvider
    {
        private SafeDateTime _blockTime;
        public SafeDateTime GetBlockTime()
        {
            return _blockTime == default ? DateTime.UtcNow.ToSafeDateTime() : _blockTime;
        }

        public void SetBlockTime(SafeDateTime blockTime)
        {
            _blockTime = blockTime;
        }
    }
}