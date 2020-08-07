using System.Diagnostics;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using Google.Protobuf.WellKnownTypes;

namespace AElf.ContractTestBase.ContractTestKit
{
    /// <summary>
    /// Some contract method's result based on the calling time,
    /// like GetConsensusCommand method of consensus contract.
    /// </summary>
    public interface IBlockTimeProvider
    {
        Timestamp GetBlockTime();
        void SetBlockTime(Timestamp blockTime);
        void SetBlockTime(int offsetMilliseconds);
    }

    public class BlockTimeProvider : IBlockTimeProvider
    {
        private Timestamp _blockTime;
        public Timestamp GetBlockTime()
        {
            return _blockTime == null ? TimestampHelper.GetUtcNow() : _blockTime;
        }

        public void SetBlockTime(Timestamp blockTime)
        {
            Debug.WriteLine($"Update block time: {blockTime}");
            _blockTime = blockTime;
        }

        public void SetBlockTime(int offsetMilliseconds)
        {
            SetBlockTime(_blockTime.AddMilliseconds(offsetMilliseconds));
        }
    }
}