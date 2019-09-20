using System;
using System.Linq;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
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

            var blockchainMiningStatusEvaluator = new BlockchainMiningStatusEvaluator(libRoundNumber,
                currentRoundNumber, AEDPoSContractConstants.MaximumTinyBlocksCount);
            blockchainMiningStatusEvaluator.Deconstruct(out var blockchainMiningStatus);

            Context.LogDebug(() => $"Current blockchain mining status: {blockchainMiningStatus.ToString()}");

            // If R_LIB + 2 < R < R_LIB + CB1, CB goes to Min(T(L2 * (CB1 - (R - R_LIB)) / A), CB0), while CT stays same as before.
            if (blockchainMiningStatus == BlockchainMiningStatus.Abnormal)
            {
                var previousRoundMinedMinerList = State.MinedMinerListMap[currentRoundNumber.Sub(1)].Pubkeys;
                var previousPreviousRoundMinedMinerList = State.MinedMinerListMap[currentRoundNumber.Sub(2)].Pubkeys;
                var minersOfLastTwoRounds = previousRoundMinedMinerList
                    .Intersect(previousPreviousRoundMinedMinerList).Count();
                var factor = minersOfLastTwoRounds.Mul(
                    blockchainMiningStatusEvaluator.SevereStatusRoundsThreshold.Sub(
                        (int) currentRoundNumber.Sub(libRoundNumber)));
                var count = Math.Min(AEDPoSContractConstants.MaximumTinyBlocksCount,
                    Ceiling(factor, currentRound.RealTimeMinersInformation.Count));
                Context.LogDebug(() => $"Maximum blocks count tune to {count}");
                return count;
            }

            //If R >= R_LIB + CB1, CB goes to 1, and CT goes to 0
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

        private static int Ceiling(int num1, int num2)
        {
            var flag = num1 % num2;
            return flag == 0 ? num1.Div(num2) : num1.Div(num2).Add(1);
        }

        internal class BlockchainMiningStatusEvaluator
        {
            private const int AbnormalThresholdRoundsCount = 2;

            /// <summary>
            /// Stands for R_LIB
            /// </summary>
            private readonly long _libRoundNumber;

            /// <summary>
            /// Stands for R
            /// </summary>
            private readonly long _currentRoundNumber;

            /// <summary>
            /// Stands for CB0
            /// </summary>
            private readonly int _maximumTinyBlocksCount;

            /// <summary>
            /// Stands for CB1
            /// </summary>
            public int SevereStatusRoundsThreshold => Math.Max(8, _maximumTinyBlocksCount);

            public BlockchainMiningStatusEvaluator(long currentConfirmedIrreversibleBlockRoundNumber,
                long currentRoundNumber, int maximumTinyBlocksCount)
            {
                _libRoundNumber = currentConfirmedIrreversibleBlockRoundNumber;
                _currentRoundNumber = currentRoundNumber;
                _maximumTinyBlocksCount = maximumTinyBlocksCount;
            }

            public void Deconstruct(out BlockchainMiningStatus status)
            {
                status = BlockchainMiningStatus.Normal;

                if (_libRoundNumber.Add(AbnormalThresholdRoundsCount) < _currentRoundNumber &&
                    _currentRoundNumber < _libRoundNumber.Add(SevereStatusRoundsThreshold))
                {
                    status = BlockchainMiningStatus.Abnormal;
                }

                if (_currentRoundNumber >= _libRoundNumber.Add(SevereStatusRoundsThreshold))
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