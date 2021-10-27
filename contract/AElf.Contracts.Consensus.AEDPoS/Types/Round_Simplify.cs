using System.Collections.Generic;
using System.Linq;

namespace AElf.Contracts.Consensus.AEDPoS
{
    /// <summary>
    /// Mirror of Round_Recover
    /// </summary>
    public partial class Round
    {
        public Round GetUpdateValueRound(string pubkey)
        {
            var minerInRound = RealTimeMinersInformation[pubkey];
            var round = new Round
            {
                RoundNumber = RoundNumber,
                RoundIdForValidation = RoundId,
                RealTimeMinersInformation =
                {
                    [pubkey] = new MinerInRound
                    {
                        Pubkey = pubkey,
                        OutValue = minerInRound.OutValue,
                        Signature = minerInRound.Signature,
                        ProducedBlocks = minerInRound.ProducedBlocks,
                        ProducedTinyBlocks = minerInRound.ProducedTinyBlocks,
                        PreviousInValue = minerInRound.PreviousInValue,
                        ActualMiningTimes = {minerInRound.ActualMiningTimes},
                        ImpliedIrreversibleBlockHeight = minerInRound.ImpliedIrreversibleBlockHeight,
                        Order = minerInRound.Order,
                        IsExtraBlockProducer = minerInRound.IsExtraBlockProducer
                    }
                }
            };
            foreach (var information in RealTimeMinersInformation)
            {
                if (information.Key == pubkey)
                {
                    round.RealTimeMinersInformation[pubkey].SupposedOrderOfNextRound =
                        minerInRound.SupposedOrderOfNextRound;
                    round.RealTimeMinersInformation[pubkey].FinalOrderOfNextRound = minerInRound.FinalOrderOfNextRound;
                }
                else
                {
                    round.RealTimeMinersInformation.Add(information.Key, new MinerInRound
                    {
                        Pubkey = information.Value.Pubkey,
                        SupposedOrderOfNextRound = information.Value.SupposedOrderOfNextRound,
                        FinalOrderOfNextRound = information.Value.FinalOrderOfNextRound,
                        Order = information.Value.Order,
                        IsExtraBlockProducer = information.Value.IsExtraBlockProducer,
                        PreviousInValue = information.Value.PreviousInValue
                    });
                }
            }

            return round;
        }

        public Round GetTinyBlockRound(string pubkey)
        {
            var minerInRound = RealTimeMinersInformation[pubkey];
            var round = new Round
            {
                RoundNumber = RoundNumber,
                RoundIdForValidation = RoundId,
                RealTimeMinersInformation =
                {
                    [pubkey] = new MinerInRound
                    {
                        Pubkey = minerInRound.Pubkey,
                        ActualMiningTimes = {minerInRound.ActualMiningTimes},
                        ProducedBlocks = minerInRound.ProducedBlocks,
                        ProducedTinyBlocks = minerInRound.ProducedTinyBlocks,
                        ImpliedIrreversibleBlockHeight = minerInRound.ImpliedIrreversibleBlockHeight
                    }
                }
            };

            foreach (var otherPubkey in RealTimeMinersInformation.Keys.Except(new List<string> {pubkey}))
            {
                round.RealTimeMinersInformation.Add(otherPubkey, new MinerInRound());
            }

            return round;
        }
    }
}