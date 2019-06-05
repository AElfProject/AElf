using System;
using System.Linq;
using Acs6;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        /// <summary>
        /// In AEDPoS, we calculate next several continual previous_in_values to provide random hash.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override RandomNumberOrder RequestRandomNumber(Empty input)
        {
            var tokenHash = Context.TransactionId;
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                var lastMinedBlockMinerInformation = currentRound.RealTimeMinersInformation.Values.OrderBy(i => i.Order)
                    .LastOrDefault(i => i.OutValue != null);

                var lastMinedBlockSlotOrder = lastMinedBlockMinerInformation?.Order ?? 0;

                var minersCount = currentRound.RealTimeMinersInformation.Count;
                // At most need to wait one round.
                var waitingBlocks = minersCount.Sub(lastMinedBlockSlotOrder).Add(1).Mul(AEDPoSContractConstants.TinyBlocksNumber);
                var expectedBlockHeight = Context.CurrentHeight.Add(waitingBlocks);
                State.RandomNumberInformationMap[tokenHash] = new RandomNumberRequestInformation
                {
                    RoundNumber = currentRound.RoundNumber,
                    Order = lastMinedBlockSlotOrder,
                    ExpectedBlockHeight = expectedBlockHeight
                };
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
                    BlockHeight = expectedBlockHeight,
                    TokenHash = tokenHash
                };
            }

            Assert(false, "Failed to get current round information");

            // Won't reach here.
            return new RandomNumberOrder
            {
                BlockHeight = long.MaxValue
            };
        }

        public override Hash GetRandomNumber(Hash input)
        {
            var roundNumberRequestInformation = State.RandomNumberInformationMap[input];
            if (roundNumberRequestInformation == null || roundNumberRequestInformation.RoundNumber == 0)
            {
                Assert(false, "Random number token not found.");
                // Won't reach here.
                return Hash.Empty;
            }

            if (roundNumberRequestInformation.ExpectedBlockHeight > Context.CurrentHeight)
            {
                Assert(false, "Still preparing random number.");
            }

            var targetRoundNumber = roundNumberRequestInformation.RoundNumber;
            if (TryToGetRoundInformation(targetRoundNumber, out var targetRound))
            {
                var neededParticipatorCount = Math.Min(AEDPoSContractConstants.RandomNumberRequestMinersCount,
                    targetRound.RealTimeMinersInformation.Count);
                var participators = targetRound.RealTimeMinersInformation.Values.Where(i =>
                    i.Order > roundNumberRequestInformation.Order && i.PreviousInValue != null).ToList();
                var roundNumber = targetRoundNumber;
                TryToGetRoundNumber(out var currentRoundNumber);
                while (participators.Count < neededParticipatorCount && roundNumber <= currentRoundNumber)
                {
                    roundNumber++;
                    if (TryToGetRoundInformation(roundNumber, out var round))
                    {
                        var newParticipators = round.RealTimeMinersInformation.Values.OrderBy(i => i.Order)
                            .Where(i => i.PreviousInValue != null).ToList();
                        var stillNeed = neededParticipatorCount - participators.Count;
                        participators.AddRange(newParticipators.Count > stillNeed
                            ? newParticipators.Take(stillNeed)
                            : newParticipators);
                    }
                    else
                    {
                        Assert(false, "Still preparing random number, try later.");
                    }
                }
                
                var inValues = participators.Select(i => i.PreviousInValue).ToList();
                var randomHash = inValues.First();
                randomHash = inValues.Skip(1).Aggregate(randomHash, Hash.FromTwoHashes);
                return randomHash;
            }

            Assert(false, "Still preparing random number, try later.");

            // Won't reach here.
            return Hash.Empty;
        }

        private void ClearTimeoutRandomNumberTokens()
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