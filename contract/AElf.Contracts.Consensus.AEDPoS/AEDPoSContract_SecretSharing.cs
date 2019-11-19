using System.Linq;
using AElf.Cryptography.SecretSharing;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.Consensus.AEDPoS
{
    // ReSharper disable once InconsistentNaming
    public partial class AEDPoSContract
    {
        private void RevealSharedInValues(Round currentRound, string publicKey)
        {
            if (!currentRound.RealTimeMinersInformation.ContainsKey(publicKey)) return;

            if (!TryToGetPreviousRoundInformation(out var previousRound)) return;

            var minersCount = currentRound.RealTimeMinersInformation.Count;
            var minimumCount = minersCount.Mul(2).Div(3);
            minimumCount = minimumCount == 0 ? 1 : minimumCount;

            foreach (var pair in previousRound.RealTimeMinersInformation.OrderBy(m => m.Value.Order))
            {
                // Skip himself.
                if (pair.Key == publicKey) continue;

                var publicKeyOfAnotherMiner = pair.Key;
                var anotherMinerInPreviousRound = pair.Value;

                if (anotherMinerInPreviousRound.EncryptedPieces.Count < minimumCount) continue;
                if (anotherMinerInPreviousRound.DecryptedPieces.Count < minersCount) continue;

                // Reveal another miner's in value for target round:

                var orders = anotherMinerInPreviousRound.DecryptedPieces.Select((t, i) =>
                        previousRound.RealTimeMinersInformation.Values
                            .First(m => m.Pubkey ==
                                        anotherMinerInPreviousRound.DecryptedPieces.Keys.ToList()[i]).Order)
                    .ToList();

                var sharedParts = anotherMinerInPreviousRound.DecryptedPieces.Values.ToList()
                    .Select(s => s.ToByteArray()).ToList();

                var revealedInValue =
                    Hash.FromRawBytes(SecretSharingHelper.DecodeSecret(sharedParts, orders, minimumCount));

                Context.LogDebug(() =>
                    $"Revealed in value of {publicKeyOfAnotherMiner} of round {previousRound.RoundNumber}: {revealedInValue}");

                currentRound.RealTimeMinersInformation[publicKeyOfAnotherMiner].PreviousInValue = revealedInValue;
            }
        }
    }
}