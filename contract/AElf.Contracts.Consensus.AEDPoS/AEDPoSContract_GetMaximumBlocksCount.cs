using System;
using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public override SInt32Value GetMaximumBlocksCount(Empty input)
        {
            return new SInt32Value {Value = GetMaximumBlocksCount()};
        }

        /// <summary>
        /// Implemented GitHub PR #1952.
        /// Adjust (mainly reduce) the count of tiny blocks produced by a miner each time to avoid too many forks.
        /// </summary>
        /// <returns></returns>
        private int GetMaximumBlocksCount()
        {
            TryToGetCurrentRoundInformation(out var currentRound, true);
            var libRoundNumber = currentRound.ConfirmedIrreversibleBlockRoundNumber;
            var libBlockHeight = currentRound.ConfirmedIrreversibleBlockHeight;
            var currentHeight = Context.CurrentHeight;
            var currentRoundNumber = currentRound.RoundNumber;

            Context.LogDebug(() =>
                $"Calculating max blocks count based on:\nR_LIB: {libRoundNumber}\nH_LIB:{libBlockHeight}\nR:{currentRoundNumber}\nH:{currentHeight}");

            if (libRoundNumber == 0 || currentRound.IsMinerListJustChanged)
            {
                Context.LogDebug(() => $"Current blockchain mining status: {BlockchainMiningStatus.Normal}");
                return AEDPoSContractConstants.MaximumTinyBlocksCount;
            }

            new BlockchainMiningStatusEvaluator(
                libRoundNumber,
                libBlockHeight,
                currentRoundNumber,
                currentHeight).Deconstruct(out var blockchainMiningStatus);

            Context.LogDebug(() => $"Current blockchain mining status: {blockchainMiningStatus.ToString()}");

            // If R_LIB + 2 < R <= R_LIB + 10 & H <= H_LIB + Y, CB goes to Min(L2 / (R - R_LIB), CB0), while CT stays same as before.
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