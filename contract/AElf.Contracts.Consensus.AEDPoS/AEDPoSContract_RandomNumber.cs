using System.Linq;
using Acs6;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        public override RandomNumberOrder RequestRandomNumber(Empty input)
        {
            var tokenHash = Context.TransactionId;
            if (TryToGetCurrentRoundInformation(out var currentRound))
            {
                var minersCount = currentRound.RealTimeMinersInformation.Count;

                var lastMinedBlockMinerInformation = currentRound.RealTimeMinersInformation.Values.OrderBy(i => i.Order)
                    .LastOrDefault(i => i.OutValue != null);
                
                var lastMinedBlockSlotOrder = lastMinedBlockMinerInformation?.Order ?? 0;

                // Extra block time slot included.
                var remainTimeSlot = minersCount.Sub(lastMinedBlockSlotOrder).Add(1);

                if (lastMinedBlockSlotOrder < AEDPoSContractConstants.RandomNumberRequestMinersCount)
                {
                    State.RandomNumberRoundMap[tokenHash] = currentRound.RoundNumber;
                    return new RandomNumberOrder
                    {
                        BlockHeight = Context.CurrentHeight.Add(remainTimeSlot.Mul(AEDPoSContractConstants.TinyBlocksNumber)),
                        Token = tokenHash
                    };
                }

                // Wait one more round.
                State.RandomNumberRoundMap[tokenHash] = currentRound.RoundNumber.Add(1);
                return new RandomNumberOrder
                {
                    BlockHeight = Context.CurrentHeight.Add(remainTimeSlot.Add(minersCount).Mul(AEDPoSContractConstants.TinyBlocksNumber)),
                    Token = tokenHash
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
            var targetRoundNumber = State.RandomNumberRoundMap[input];
            if (targetRoundNumber == 0)
            {
                Assert(false, "Random number token not found.");
            }

            if (TryToGetRoundNumber(out var currentRoundNumber) && currentRoundNumber <= targetRoundNumber)
            {
                Assert(false, "Still preparing random number.");
            }

            if (TryToGetRoundInformation(targetRoundNumber, out var targetRound))
            {
                
            }
        }
    }
}