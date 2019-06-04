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
            if (roundNumberRequestInformation == null)
            {
                Assert(false, "Random number token not found.");
                // Won't reach here.
                return Hash.Empty;
            }

            if (roundNumberRequestInformation.ExpectedBlockHeight < Context.CurrentHeight)
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
                        if (newParticipators.Count > stillNeed)
                        {
                            participators.AddRange(newParticipators.Take(stillNeed));
                        }
                        else
                        {
                            participators.AddRange(newParticipators);
                        }
                    }
                    else
                    {
                        Assert(false, "Still preparing random number, try later.");
                    }
                }
                
                // Now we can delete this token_hash from RandomNumberInformationMap
                // TODO: Set null if deleting key supported.
                State.RandomNumberInformationMap[input] = new RandomNumberRequestInformation();

                var inValues = participators.Select(i => i.PreviousInValue).ToList();
                var randomHash = inValues.First();
                randomHash = inValues.Skip(1).Aggregate(randomHash, Hash.FromTwoHashes);
                return randomHash;
            }

            Assert(false, "Still preparing random number, try later.");

            // Won't reach here.
            return Hash.Empty;
        }
    }
}