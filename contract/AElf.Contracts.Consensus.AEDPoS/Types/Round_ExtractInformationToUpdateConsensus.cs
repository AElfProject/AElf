using System.Linq;
using AElf.Types;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class Round
    {
        /// <summary>
        /// Maybe tune other miners' supposed order of next round,
        /// will record this purpose to their FinalOrderOfNextRound field.
        /// </summary>
        /// <param name="pubkey"></param>
        /// <returns></returns>
        public UpdateValueInput ExtractInformationToUpdateConsensus(string pubkey)
        {
            if (!RealTimeMinersInformation.ContainsKey(pubkey))
            {
                return null;
            }

            var minerInRound = RealTimeMinersInformation[pubkey];

            var tuneOrderInformation = RealTimeMinersInformation.Values
                .Where(m => m.FinalOrderOfNextRound != m.SupposedOrderOfNextRound)
                .ToDictionary(m => m.Pubkey, m => m.FinalOrderOfNextRound);

            var decryptedPreviousInValues = RealTimeMinersInformation.Values.Where(v =>
                    v.Pubkey != pubkey && v.DecryptedPreviousInValues.ContainsKey(pubkey))
                .ToDictionary(info => info.Pubkey, info => info.DecryptedPreviousInValues[pubkey]);

            var minersPreviousInValues =
                RealTimeMinersInformation.Values.Where(info => info.PreviousInValue != null).ToDictionary(
                    info => info.Pubkey,
                    info => info.PreviousInValue);

            return new UpdateValueInput
            {
                OutValue = minerInRound.OutValue,
                Signature = minerInRound.Signature,
                PreviousInValue = minerInRound.PreviousInValue ?? Hash.Empty,
                RoundId = RoundId,
                ProducedBlocks = minerInRound.ProducedBlocks,
                ActualMiningTime = minerInRound.ActualMiningTimes.Last(),
                SupposedOrderOfNextRound = minerInRound.SupposedOrderOfNextRound,
                TuneOrderInformation = {tuneOrderInformation},
                EncryptedInValues = {minerInRound.EncryptedInValues},
                DecryptedPreviousInValues = {decryptedPreviousInValues},
                MinersPreviousInValues = {minersPreviousInValues},
                ImpliedIrreversibleBlockHeight = minerInRound.ImpliedIrreversibleBlockHeight
            };
        }
    }
}