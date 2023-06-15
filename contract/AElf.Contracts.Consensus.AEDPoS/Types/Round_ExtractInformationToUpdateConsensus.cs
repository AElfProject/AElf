using System.Linq;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.Consensus.AEDPoS;

public partial class Round
{
    /// <summary>
    ///     Maybe tune other miners' supposed order of next round,
    ///     will record this purpose to their FinalOrderOfNextRound field.
    /// </summary>
    /// <param name="pubkey"></param>
    /// <param name="randomNumber"></param>
    /// <returns></returns>
    public UpdateValueInput ExtractInformationToUpdateConsensus(string pubkey, ByteString randomNumber)
    {
        if (!RealTimeMinersInformation.ContainsKey(pubkey)) return null;

        var minerInRound = RealTimeMinersInformation[pubkey];

        var tuneOrderInformation = RealTimeMinersInformation.Values
            .Where(m => m.FinalOrderOfNextRound != m.SupposedOrderOfNextRound)
            .ToDictionary(m => m.Pubkey, m => m.FinalOrderOfNextRound);

        var decryptedPreviousInValues = RealTimeMinersInformation.Values.Where(v =>
                v.Pubkey != pubkey && v.DecryptedPieces.ContainsKey(pubkey))
            .ToDictionary(info => info.Pubkey, info => info.DecryptedPieces[pubkey]);

        var minersPreviousInValues =
            RealTimeMinersInformation.Values.Where(info => info.PreviousInValue != null).ToDictionary(
                info => info.Pubkey,
                info => info.PreviousInValue);

        return new UpdateValueInput
        {
            OutValue = minerInRound.OutValue,
            Signature = minerInRound.Signature,
            PreviousInValue = minerInRound.PreviousInValue ?? Hash.Empty,
            RoundId = RoundIdForValidation,
            ProducedBlocks = minerInRound.ProducedBlocks,
            ActualMiningTime = minerInRound.ActualMiningTimes.Last(),
            SupposedOrderOfNextRound = minerInRound.SupposedOrderOfNextRound,
            TuneOrderInformation = { tuneOrderInformation },
            EncryptedPieces = { minerInRound.EncryptedPieces },
            DecryptedPieces = { decryptedPreviousInValues },
            MinersPreviousInValues = { minersPreviousInValues },
            ImpliedIrreversibleBlockHeight = minerInRound.ImpliedIrreversibleBlockHeight,
            RandomNumber = randomNumber
        };
    }
}