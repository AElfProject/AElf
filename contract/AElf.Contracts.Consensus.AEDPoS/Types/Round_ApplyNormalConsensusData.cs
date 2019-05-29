using System;
using System.Linq;
using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    internal partial class Round
    {
        public Round ApplyNormalConsensusData(string publicKey, Hash previousInValue,
            Hash outValue, Hash signature)
        {
            if (!RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return this;
            }

            RealTimeMinersInformation[publicKey].OutValue = outValue;
            RealTimeMinersInformation[publicKey].Signature = signature;
            if (previousInValue != Hash.Empty)
            {
                RealTimeMinersInformation[publicKey].PreviousInValue = previousInValue;
            }

            var minersCount = RealTimeMinersInformation.Count;
            var sigNum =
                BitConverter.ToInt64(
                    BitConverter.IsLittleEndian ? signature.Value.Reverse().ToArray() : signature.Value.ToArray(),
                    0);
            var supposedOrderOfNextRound = GetAbsModulus(sigNum, minersCount) + 1;

            // Check the existence of conflicts about OrderOfNextRound.
            // If so, modify others'.
            var conflicts = RealTimeMinersInformation.Values
                .Where(i => i.FinalOrderOfNextRound == supposedOrderOfNextRound).ToList();

            foreach (var orderConflictedMiner in conflicts)
            {
                // Though multiple conflicts should be wrong, we can still arrange their orders of next round.

                for (var i = supposedOrderOfNextRound + 1; i < minersCount * 2; i++)
                {
                    var maybeNewOrder = i > minersCount ? i % minersCount : i;
                    if (RealTimeMinersInformation.Values.All(m => m.FinalOrderOfNextRound != maybeNewOrder))
                    {
                        RealTimeMinersInformation[orderConflictedMiner.PublicKey].FinalOrderOfNextRound =
                            maybeNewOrder;
                        break;
                    }
                }
            }

            RealTimeMinersInformation[publicKey].SupposedOrderOfNextRound = supposedOrderOfNextRound;
            // Initialize FinalOrderOfNextRound as the value of SupposedOrderOfNextRound
            RealTimeMinersInformation[publicKey].FinalOrderOfNextRound = supposedOrderOfNextRound;

            return this;
        }
    }
}