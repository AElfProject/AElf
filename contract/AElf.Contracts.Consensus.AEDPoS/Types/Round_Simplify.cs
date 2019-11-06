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
            var result = new Round
            {
                RoundIdForValidation = RoundId,
                RealTimeMinersInformation =
                {
                    [pubkey] = new MinerInRound
                    {
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
                    result.RealTimeMinersInformation[pubkey].SupposedOrderOfNextRound =
                        minerInRound.SupposedOrderOfNextRound;
                    result.RealTimeMinersInformation[pubkey].FinalOrderOfNextRound = minerInRound.FinalOrderOfNextRound;
                }
                else
                {
                    result.RealTimeMinersInformation.Add(information.Key, new MinerInRound
                    {
                        Pubkey = information.Value.Pubkey,
                        SupposedOrderOfNextRound = information.Value.SupposedOrderOfNextRound,
                        FinalOrderOfNextRound = information.Value.FinalOrderOfNextRound,
                        Order = information.Value.Order,
                        IsExtraBlockProducer = information.Value.IsExtraBlockProducer
                    });
                }
            }

            return result;
        }

        public Round GetTinyBlockRound(string pubkey)
        {
            var minerInRound = RealTimeMinersInformation[pubkey];
            return new Round
            {
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
        }
    }
}