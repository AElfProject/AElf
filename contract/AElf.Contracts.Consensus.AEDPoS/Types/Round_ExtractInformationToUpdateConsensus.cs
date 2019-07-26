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
        /// <param name="publicKey"></param>
        /// <returns></returns>
        public UpdateValueInput ExtractInformationToUpdateConsensus(string publicKey)
        {
            if (!RealTimeMinersInformation.ContainsKey(publicKey))
            {
                return null;
            }

            var minerInRound = RealTimeMinersInformation[publicKey];

            var tuneOrderInformation = RealTimeMinersInformation.Values
                .Where(m => m.FinalOrderOfNextRound != m.SupposedOrderOfNextRound)
                .ToDictionary(m => m.Pubkey, m => m.FinalOrderOfNextRound);

            var decryptedPreviousInValues = RealTimeMinersInformation.Values.Where(v =>
                    v.Pubkey != publicKey && v.DecryptedPreviousInValues.ContainsKey(publicKey))
                .ToDictionary(info => info.Pubkey, info => info.DecryptedPreviousInValues[publicKey]);

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