using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        internal class BlockchainMiningStatusEvaluator
        {
            private const int CachedBlocksCount = 1024; // Stands for Y

            /// <summary>
            /// Stands for R_LIB
            /// </summary>
            private readonly long _libRoundNumber;

            /// <summary>
            /// Stands for H_LIB
            /// </summary>
            private readonly long _libBlockHeight;

            /// <summary>
            /// Stands for R
            /// </summary>
            private readonly long _currentRoundNumber;

            /// <summary>
            /// Stands for H
            /// </summary>
            private readonly long _currentBlockHeight;

            public BlockchainMiningStatusEvaluator(long currentConfirmedIrreversibleBlockRoundNumber,
                long currentConfirmedIrreversibleBlockHeight, long currentRoundNumber, long currentBlockHeight)
            {
                _libRoundNumber = currentConfirmedIrreversibleBlockRoundNumber;
                _libBlockHeight = currentConfirmedIrreversibleBlockHeight;
                _currentRoundNumber = currentRoundNumber;
                _currentBlockHeight = currentBlockHeight;
            }

            public void Deconstruct(out BlockchainMiningStatus status)
            {
                status = BlockchainMiningStatus.Normal;

                if (_libRoundNumber.Add(2) < _currentRoundNumber && _currentRoundNumber <= _libRoundNumber.Add(10) &&
                    _currentBlockHeight <= _libBlockHeight.Add(CachedBlocksCount))
                {
                    status = BlockchainMiningStatus.Abnormal;
                }

                if (_currentRoundNumber > _libRoundNumber.Add(10) ||
                    _currentBlockHeight > _libBlockHeight.Add(CachedBlocksCount))
                {
                    status = BlockchainMiningStatus.Severe;
                }
            }
        }

        internal enum BlockchainMiningStatus
        {
            Normal,
            Abnormal,
            Severe
        }
    }
}