using System;
using System.Linq;
using AElf.Sdk.CSharp;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        /// <summary>
        /// Implemented GitHub PR #1952.
        /// Adjust (mainly reduce) the count of tiny blocks produced by a miner each time to avoid too many forks.
        /// </summary>
        /// <returns></returns>
        private int GetMinimumBlocksCount()
        {
            if (_minimumBlocksCount != 0)
            {
                return _minimumBlocksCount;
            }

            _minimumBlocksCount = AEDPoSContractConstants.MaximumTinyBlocksCount;

            TryToGetCurrentRoundInformation(out var currentRound, true);
            var libRoundNumber = currentRound.ConfirmedIrreversibleBlockRoundNumber;
            var libBlockHeight = currentRound.ConfirmedIrreversibleBlockHeight;
            var currentHeight = Context.CurrentHeight;
            var currentRoundNumber = currentRound.RoundNumber;

            if (libRoundNumber == 0)
            {
                return AEDPoSContractConstants.MaximumTinyBlocksCount;
            }

            var (blockchainMiningStatus) =
                new BlockchainMiningStatusEvaluator(
                    libRoundNumber,
                    libBlockHeight,
                    currentRoundNumber,
                    currentHeight);

            Context.LogDebug(() => $"Current blockchain mining status: {blockchainMiningStatus.ToString()}");

            // If R_LIB + 2 < R < R_LIB + 10 & H <= H_LIB + Y, CB goes to Min(L2 / (R - R_LIB), CB0), while CT stays same as before.
            if (blockchainMiningStatus == BlockchainMiningStatus.Abnormal)
            {
                var previousRoundMinedMinerList = State.MinedMinerListMap[currentRoundNumber.Sub(1)].Pubkeys;
                var previousPreviousRoundMinedMinerList = State.MinedMinerListMap[currentRoundNumber.Sub(2)].Pubkeys;
                var minersOfLastTwoRounds = previousRoundMinedMinerList
                    .Intersect(previousPreviousRoundMinedMinerList).Count();
                var count = Math.Min(AEDPoSContractConstants.MaximumTinyBlocksCount, minersOfLastTwoRounds
                    .Div((int) currentRound.RoundNumber.Sub(libRoundNumber))
                    .Add(1));
                Context.LogDebug(() => $"Maximum blocks count tune to {count}");
                _minimumBlocksCount = count;
                return count;
            }

            //If R > R_LIB + 10 || H > H_LIB + Y, CB goes to 1, and CT goes to 0
            if (blockchainMiningStatus == BlockchainMiningStatus.Severe)
            {
                // Fire an event to notify miner not package normal transaction.
                Context.Fire(new IrreversibleBlockHeightUnacceptable
                {
                    DistanceToIrreversibleBlockHeight = currentHeight.Sub(libBlockHeight)
                });
                _minimumBlocksCount = 1;
                return 1;
            }

            return AEDPoSContractConstants.MaximumTinyBlocksCount;
        }

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