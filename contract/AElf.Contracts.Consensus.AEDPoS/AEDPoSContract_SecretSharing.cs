using System.Linq;
using System.Text;
using AElf.Cryptography.SecretSharing;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Contracts.Consensus.AEDPoS
{
    public partial class AEDPoSContract
    {
        private void ShareInValueOfCurrentRound(Round currentRound, Round previousRound, Hash inValue, string publicKey)
        {
            if (!currentRound.RealTimeMinersInformation.ContainsKey(publicKey)) return;

            var minersCount = currentRound.RealTimeMinersInformation.Count;
            var minimumCount = minersCount.Mul(2).Div(3);
            minimumCount = minimumCount == 0 ? 1 : minimumCount;

            var secretShares = SecretSharingHelper.EncodeSecret(inValue.ToByteArray(), minimumCount, minersCount);
            foreach (var pair in currentRound.RealTimeMinersInformation.OrderBy(m => m.Value.Order)
                .ToDictionary(m => m.Key, m => m.Value.Order))
            {
                // Skip himself.
                if (pair.Key == publicKey) continue;

                var publicKeyOfAnotherMiner = pair.Key;
                var orderOfAnotherMiner = pair.Value;

                // Share in value of current round:

                // Encrypt every secret share with other miner's public key, then fill EncryptedInValues field.
                var plainMessage = secretShares[orderOfAnotherMiner - 1];
                var receiverPublicKey = ByteArrayHelper.HexStringToByteArray(publicKeyOfAnotherMiner);
                var encryptedInValue = Context.EncryptMessage(receiverPublicKey, plainMessage);
                currentRound.RealTimeMinersInformation[publicKey].EncryptedInValues
                    .Add(publicKeyOfAnotherMiner, ByteString.CopyFrom(encryptedInValue));

                // Decrypt shares published during previous round:

                // First round of every term don't have previous in values.
                if (IsFirstRoundOfCurrentTerm(out _)) continue;

                // Become a miner from this round.
                if (!previousRound.RealTimeMinersInformation.ContainsKey(publicKeyOfAnotherMiner)) continue;

                // No need to decrypt shares of miners who already revealed their previous in values.
                if (currentRound.RealTimeMinersInformation[publicKeyOfAnotherMiner].PreviousInValue != null) continue;

                var encryptedShares =
                    previousRound.RealTimeMinersInformation[publicKeyOfAnotherMiner].EncryptedInValues;
                if (!encryptedShares.Any()) continue;
                var interestingMessage = encryptedShares[publicKey];
                var senderPublicKey = ByteArrayHelper.HexStringToByteArray(publicKeyOfAnotherMiner);
                // Decrypt another miner's secret share then add a result to this miner's DecryptedInValues field.
                var decryptedInValue = Context.DecryptMessage(senderPublicKey, interestingMessage.ToByteArray());
                currentRound.RealTimeMinersInformation[publicKeyOfAnotherMiner].DecryptedPreviousInValues
                    .Add(publicKey, ByteString.CopyFrom(decryptedInValue));
            }
        }

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

                if (anotherMinerInPreviousRound.EncryptedInValues.Count < minimumCount) continue;
                if (anotherMinerInPreviousRound.DecryptedPreviousInValues.Count < minersCount) continue;

                // Reveal another miner's in value for target round:

                var orders = anotherMinerInPreviousRound.DecryptedPreviousInValues.Select((t, i) =>
                        previousRound.RealTimeMinersInformation.Values
                            .First(m => m.Pubkey ==
                                        anotherMinerInPreviousRound.DecryptedPreviousInValues.Keys.ToList()[i]).Order)
                    .ToList();

                var sharedParts = anotherMinerInPreviousRound.DecryptedPreviousInValues.Values.ToList()
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