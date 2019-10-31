namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        public Round GetUpdateValueRound(string pubkey)
        {
            var minerInRound = RealTimeMinersInformation[pubkey];
            return new Round
            {
                RealTimeMinersInformation =
                {
                    [pubkey] = new MinerInRound
                    {
                        OutValue = minerInRound.OutValue,
                        Signature = minerInRound.Signature,
                        ProducedBlocks = minerInRound.ProducedBlocks,
                        ProducedTinyBlocks = minerInRound.ProducedTinyBlocks,
                        PreviousInValue = minerInRound.PreviousInValue,
                        SupposedOrderOfNextRound = minerInRound.SupposedOrderOfNextRound,
                        FinalOrderOfNextRound = minerInRound.FinalOrderOfNextRound,
                        ActualMiningTimes = {minerInRound.ActualMiningTimes},
                        ImpliedIrreversibleBlockHeight = minerInRound.ImpliedIrreversibleBlockHeight
                    }
                }
            };
        }

        public Round GetTinyBlockRound(string pubkey)
        {
            var minerInRound = RealTimeMinersInformation[pubkey];
            return new Round
            {
                RealTimeMinersInformation =
                {
                    [pubkey] = new MinerInRound
                    {
                        ActualMiningTimes = {minerInRound.ActualMiningTimes},
                        ProducedBlocks = minerInRound.ProducedBlocks,
                        ProducedTinyBlocks = minerInRound.ProducedTinyBlocks
                    }
                }
            };
        }
    }
}