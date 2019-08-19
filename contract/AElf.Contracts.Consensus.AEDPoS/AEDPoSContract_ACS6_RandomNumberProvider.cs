using System;
using System.Collections.Generic;
using System.Linq;
using Acs6;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        internal class RandomNumberRequestHandler
        {
            private readonly Round _currentRound;
            private readonly long _currentHeight;
            private readonly int _minersCount;
            private readonly int _minimumRequestMinersCount;

            /// <summary>
            /// To response to a request for random number, we need to:
            /// 1. return a token for getting that random number later,
            /// 2. record current round number
            /// </summary>
            public RandomNumberRequestHandler(Round currentRound, long currentHeight)
            {
                _currentRound = currentRound;
                _currentHeight = currentHeight;
                _minersCount = currentRound.RealTimeMinersInformation.Count;
                _minimumRequestMinersCount = _minersCount.Mul(2).Div(3).Add(1);
            }

            public RandomNumberRequestInformation GetRandomNumberRequestInformation()
            {
                var lastMinedMinerInformation = _currentRound.RealTimeMinersInformation.Values.OrderBy(i => i.Order)
                    .LastOrDefault(i => i.OutValue != null);
                var minedMinersCount = lastMinedMinerInformation?.Order ?? 0;
                var leftMinersCount = _minersCount.Sub(minedMinersCount);

                if (leftMinersCount >= _minimumRequestMinersCount)
                {
                    // It's possible for user to get random number in current round.
                    return new RandomNumberRequestInformation
                    {
                        TargetRoundNumber = _currentRound.RoundNumber,
                        ExpectedBlockHeight =
                            _currentHeight.Add(
                                _minimumRequestMinersCount.Mul(AEDPoSContractConstants.TinyBlocksNumber)),
                        Order = minedMinersCount
                    };
                }

                var leftTinyBlocks = lastMinedMinerInformation == null
                    ? 0
                    : AEDPoSContractConstants.TinyBlocksNumber.Sub(lastMinedMinerInformation.ActualMiningTimes.Count);
                var leftBlocksCount = _currentHeight.Add(leftMinersCount.Mul(AEDPoSContractConstants.TinyBlocksNumber))
                    .Add(leftTinyBlocks);
                return new RandomNumberRequestInformation
                {
                    TargetRoundNumber = _currentRound.RoundNumber.Add(1),
                    ExpectedBlockHeight = _currentHeight.Add(leftBlocksCount),
                    Order = minedMinersCount
                };
            }
        }

        internal class RandomNumberProvider
        {
            private readonly RandomNumberRequestInformation _requestInformation;

            /// <summary>
            /// The generation of random number (hash) depends on RandomNumberRequestInformation
            /// </summary>
            public RandomNumberProvider(RandomNumberRequestInformation requestInformation)
            {
                _requestInformation = requestInformation;
            }

            public Hash GetRandomNumber(Round round)
            {
                var minimumRequestMinersCount = round.RealTimeMinersInformation.Count.Mul(2).Div(3).Add(1);
                List<MinerInRound> participators;
                if (round.RoundNumber == _requestInformation.TargetRoundNumber)
                {
                    participators = round.RealTimeMinersInformation.Values.Where(i =>
                        i.Order > _requestInformation.Order && i.PreviousInValue != null).ToList();
                }
                else
                {
                    participators = round.RealTimeMinersInformation.Values.Where(i => i.PreviousInValue != null)
                        .ToList();
                }

                if (participators.Count >= minimumRequestMinersCount)
                {
                    var inValues = participators.Select(i => i.PreviousInValue).ToList();
                    var randomHash = inValues.First();
                    randomHash = inValues.Skip(1).Aggregate(randomHash, Hash.FromTwoHashes);
                    return randomHash;
                }

                return Hash.Empty;
            }
        }

        /// <summary>
        /// In AEDPoS, we calculate next several continual previous_in_values to provide random hash.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override RandomNumberOrder RequestRandomNumber(RequestRandomNumberInput input)
        {
            var tokenHash = Context.TransactionId;
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                var information = new RandomNumberRequestHandler(currentRound, Context.CurrentHeight)
                    .GetRandomNumberRequestInformation();
                State.RandomNumberInformationMap[tokenHash] = information;

                // For clear usage.
                if (State.RandomNumberTokenMap[currentRound.RoundNumber] == null)
                {
                    State.RandomNumberTokenMap[currentRound.RoundNumber] = new HashList {Values = {tokenHash}};
                }
                else
                {
                    State.RandomNumberTokenMap[currentRound.RoundNumber].Values.Add(tokenHash);
                }

                return new RandomNumberOrder
                {
                    BlockHeight = information.ExpectedBlockHeight,
                    TokenHash = tokenHash
                };
            }

            // Not possible.
            Assert(false, "Failed to get current round information");

            // Won't reach here anyway.
            return new RandomNumberOrder
            {
                BlockHeight = long.MaxValue
            };
        }

        public override Hash GetRandomNumber(Hash input)
        {
            var randomNumberRequestInformation = State.RandomNumberInformationMap[input];
            if (randomNumberRequestInformation == null || randomNumberRequestInformation.TargetRoundNumber == 0)
            {
                Assert(false, "Random number token not found.");
                // Won't reach here.
                return Hash.Empty;
            }

            if (randomNumberRequestInformation.ExpectedBlockHeight > Context.CurrentHeight)
            {
                Assert(false, "Still preparing random number.");
            }

            var roundNumber = randomNumberRequestInformation.TargetRoundNumber;
            TryToGetRoundNumber(out var currentRoundNumber);
            var provider = new RandomNumberProvider(randomNumberRequestInformation);
            while (roundNumber <= currentRoundNumber)
            {
                if (TryToGetRoundInformation(roundNumber, out var round))
                {
                    var randomHash = provider.GetRandomNumber(round);
                    if (randomHash != Hash.Empty)
                    {
                        return randomHash;
                    }

                    roundNumber = roundNumber.Add(1);
                }
                else
                {
                    Assert(false, "Still preparing random number, try later.");
                }
            }

            Assert(false, "Still preparing random number, try later.");

            // Won't reach here.
            return Hash.Empty;
        }

        private void ClearExpiredRandomNumberTokens()
        {
            if (!TryToGetCurrentRoundInformation(out var currentRound)) return;

            if (currentRound.RoundNumber <= AEDPoSContractConstants.RandomNumberDueRoundCount)
            {
                return;
            }

            var targetRoundNumber = currentRound.RoundNumber.Sub(AEDPoSContractConstants.RandomNumberDueRoundCount);
            var tokens = State.RandomNumberTokenMap[targetRoundNumber];
            if (tokens == null || !tokens.Values.Any()) return;

            foreach (var token in tokens.Values)
            {
                // TODO: Set to null.
                State.RandomNumberInformationMap[token] = new RandomNumberRequestInformation();
            }

            // TODO: Set to null.
            State.RandomNumberTokenMap[targetRoundNumber] = new HashList();
        }
    }
}