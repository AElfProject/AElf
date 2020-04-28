using System.Collections.Generic;
using System.Linq;
using Acs6;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        private class RandomNumberRequestHandler
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
                _minersCount = currentRound.RealTimeMinersInformation.Keys.Count;
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
                                _minimumRequestMinersCount.Mul(AEDPoSContractConstants.MaximumTinyBlocksCount)),
                        Order = minedMinersCount
                    };
                }

                var leftTinyBlocks = 0;
                if (lastMinedMinerInformation != null)
                {
                    var lastMinedMinerPubkey = lastMinedMinerInformation.Pubkey;
                    leftTinyBlocks = _currentRound.ExtraBlockProducerOfPreviousRound == lastMinedMinerPubkey
                        ? AEDPoSContractConstants.MaximumTinyBlocksCount.Mul(2).Sub(lastMinedMinerInformation
                            .ActualMiningTimes.Count)
                        : AEDPoSContractConstants.MaximumTinyBlocksCount.Sub(lastMinedMinerInformation.ActualMiningTimes
                            .Count);
                }

                var leftBlocksCount = leftMinersCount.Mul(AEDPoSContractConstants.MaximumTinyBlocksCount)
                    .Add(leftTinyBlocks);
                return new RandomNumberRequestInformation
                {
                    TargetRoundNumber = _currentRound.RoundNumber.Add(1),
                    ExpectedBlockHeight = _currentHeight.Add(leftBlocksCount),
                    Order = minedMinersCount
                };
            }
        }

        private class RandomNumberProvider
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
                    randomHash = inValues.Skip(1).Aggregate(randomHash, HashHelper.ConcatAndCompute);
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
        public override RandomNumberOrder RequestRandomNumber(Hash input)
        {
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                var randomNumberCount = State.RandomNumberTokenMap[currentRound.RoundNumber]?.Values.Count ?? 0;
                var tokenHash = Context.GenerateId(Context.Self, randomNumberCount.ToBytes(false));

                var requestInformation = new RandomNumberRequestHandler(currentRound, Context.CurrentHeight)
                    .GetRandomNumberRequestInformation();

                State.RandomNumberInformationMap[tokenHash] = requestInformation;

                // For clearing tokens of certain round number.
                if (State.RandomNumberTokenMap[currentRound.RoundNumber] == null)
                {
                    State.RandomNumberTokenMap[currentRound.RoundNumber] = new HashList {Values = {tokenHash}};
                }
                else
                {
                    State.RandomNumberTokenMap[currentRound.RoundNumber].Values.Add(tokenHash);
                }

                Context.Fire(new RandomNumberRequestHandled
                {
                    Requester = Context.Sender,
                    BlockHeight = requestInformation.ExpectedBlockHeight,
                    TokenHash = tokenHash
                });

                Context.LogDebug(() =>
                    $"Handled a request of random number: {tokenHash}.current height: {Context.CurrentHeight}, target height: {requestInformation.ExpectedBlockHeight}");

                return new RandomNumberOrder
                {
                    BlockHeight = requestInformation.ExpectedBlockHeight,
                    TokenHash = tokenHash
                };
            }

            // Impossible.
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
            if (randomNumberRequestInformation == null ||
                randomNumberRequestInformation.TargetRoundNumber == 0 ||
                randomNumberRequestInformation.ExpectedBlockHeight > Context.CurrentHeight ||
                !TryToGetRoundNumber(out var currentRoundNumber))
            {
                return Hash.Empty;
            }

            var targetRoundNumber = randomNumberRequestInformation.TargetRoundNumber;
            var provider = new RandomNumberProvider(randomNumberRequestInformation);
            while (targetRoundNumber <= currentRoundNumber)
            {
                if (TryToGetRoundInformation(targetRoundNumber, out var round))
                {
                    var randomHash = provider.GetRandomNumber(round);
                    if (randomHash != Hash.Empty)
                    {
                        var finalRandomHash = HashHelper.ConcatAndCompute(randomHash, input);
                        Context.Fire(new RandomNumberGenerated {TokenHash = input, RandomHash = finalRandomHash});
                        return finalRandomHash;
                    }

                    targetRoundNumber = targetRoundNumber.Add(1);
                }
            }

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
                State.RandomNumberInformationMap.Remove(token);
            }

            State.RandomNumberTokenMap.Remove(targetRoundNumber);
        }
    }
}