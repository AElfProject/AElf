using System.Linq;
using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        public Round ApplyNormalConsensusData(string pubkey, Hash previousInValue, Hash outValue, Hash signature)
        {
            if (!RealTimeMinersInformation.ContainsKey(pubkey))
            {
                return this;
            }

            RealTimeMinersInformation[pubkey].OutValue = outValue;
            RealTimeMinersInformation[pubkey].Signature = signature;
            if (RealTimeMinersInformation[pubkey].PreviousInValue == Hash.Empty ||
                RealTimeMinersInformation[pubkey].PreviousInValue == null)
            {
                RealTimeMinersInformation[pubkey].PreviousInValue = previousInValue;
            }

            var minersCount = RealTimeMinersInformation.Count;
            var sigNum = signature.ToInt64();

            var supposedOrderOfNextRound = GetAbsModulus(sigNum, minersCount) + 1;

            // Check the existence of conflicts about OrderOfNextRound.
            // If so, modify others'.
            var conflicts = RealTimeMinersInformation.Values
                .Where(i => i.FinalOrderOfNextRound == supposedOrderOfNextRound).ToList();

            foreach (var orderConflictedMiner in conflicts)
            {
                // Multiple conflicts is unlikely.

                for (var i = supposedOrderOfNextRound + 1; i < minersCount * 2; i++)
                {
                    var maybeNewOrder = i > minersCount ? i % minersCount : i;
                    if (RealTimeMinersInformation.Values.All(m => m.FinalOrderOfNextRound != maybeNewOrder))
                    {
                        RealTimeMinersInformation[orderConflictedMiner.Pubkey].FinalOrderOfNextRound =
                            maybeNewOrder;
                        break;
                    }
                }
            }

            RealTimeMinersInformation[pubkey].SupposedOrderOfNextRound = supposedOrderOfNextRound;
            // Initialize FinalOrderOfNextRound as the value of SupposedOrderOfNextRound
            RealTimeMinersInformation[pubkey].FinalOrderOfNextRound = supposedOrderOfNextRound;

            return this;
        }
    }
}