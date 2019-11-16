namespace AElf.Contracts.Consensus.AEDPoS
{
    /// <summary>
    /// Mirror of Round_Simplify
    /// </summary>
    public partial class Round
    {
        public Round RecoverFromUpdateValue(Round providedRound, string pubkey)
        {
            if (!RealTimeMinersInformation.ContainsKey(pubkey) ||
                !providedRound.RealTimeMinersInformation.ContainsKey(pubkey))
            {
                return this;
            }

            var minerInRound = RealTimeMinersInformation[pubkey];
            var providedInformation = providedRound.RealTimeMinersInformation[pubkey];
            minerInRound.OutValue = providedInformation.OutValue;
            minerInRound.Signature = providedInformation.Signature;
            minerInRound.ProducedBlocks = providedInformation.ProducedBlocks;
            minerInRound.ProducedTinyBlocks = providedInformation.ProducedTinyBlocks;
            minerInRound.PreviousInValue = providedInformation.PreviousInValue;
            minerInRound.ImpliedIrreversibleBlockHeight = providedInformation.ImpliedIrreversibleBlockHeight;
            minerInRound.ActualMiningTimes.Add(providedInformation.ActualMiningTimes);

            foreach (var information in providedRound.RealTimeMinersInformation)
            {
                RealTimeMinersInformation[information.Key].SupposedOrderOfNextRound =
                    information.Value.SupposedOrderOfNextRound;
                RealTimeMinersInformation[information.Key].FinalOrderOfNextRound =
                    information.Value.FinalOrderOfNextRound;
            }

            return this;
        }

        public Round RecoverFromTinyBlock(Round providedRound, string pubkey)
        {
            if (!RealTimeMinersInformation.ContainsKey(pubkey) ||
                !providedRound.RealTimeMinersInformation.ContainsKey(pubkey))
            {
                return this;
            }

            var minerInRound = RealTimeMinersInformation[pubkey];
            var providedInformation = providedRound.RealTimeMinersInformation[pubkey];
            minerInRound.ProducedBlocks = providedInformation.ProducedBlocks;
            minerInRound.ProducedTinyBlocks = providedInformation.ProducedTinyBlocks;
            minerInRound.ImpliedIrreversibleBlockHeight = providedInformation.ImpliedIrreversibleBlockHeight;
            minerInRound.ActualMiningTimes.Add(providedInformation.ActualMiningTimes);

            return this;
        }
    }
}