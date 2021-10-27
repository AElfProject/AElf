using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Cryptography.SecretSharing;
using AElf.CSharp.Core;
using AElf.Kernel.Account.Application;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using AElf.CSharp.Core.Extension;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class SecretSharingService : ISecretSharingService, ISingletonDependency
    {
        private readonly Dictionary<long, Dictionary<string, byte[]>> _encryptedPieces =
            new Dictionary<long, Dictionary<string, byte[]>>();

        private readonly Dictionary<long, Dictionary<string, byte[]>> _decryptedPieces =
            new Dictionary<long, Dictionary<string, byte[]>>();

        private readonly Dictionary<long, Dictionary<string, Hash>> _revealedInValues =
            new Dictionary<long, Dictionary<string, Hash>>();

        private readonly IInValueCache _inValueCache;
        private readonly IAccountService _accountService;

        public ILogger<SecretSharingService> Logger { get; set; }

        public SecretSharingService(IInValueCache inValueCache, IAccountService accountService)
        {
            _inValueCache = inValueCache;
            _accountService = accountService;

            Logger = NullLogger<SecretSharingService>.Instance;
        }

        public async Task AddSharingInformationAsync(SecretSharingInformation secretSharingInformation)
        {
            try
            {
                var selfPubkey = (await _accountService.GetPublicKeyAsync()).ToHex();

                if (!secretSharingInformation.PreviousRound.RealTimeMinersInformation.ContainsKey(selfPubkey))
                {
                    return;
                }

                var newInValue = await GenerateInValueAsync(secretSharingInformation);
                Logger.LogDebug(
                    $"Add in value {newInValue} for round id {secretSharingInformation.CurrentRoundId}");
                _inValueCache.AddInValue(secretSharingInformation.CurrentRoundId, newInValue);

                if (secretSharingInformation.PreviousRound.RealTimeMinersInformation.Count == 1)
                {
                    return;
                }

                await CollectPiecesWithSecretSharingAsync(secretSharingInformation, newInValue, selfPubkey);
                RevealPreviousInValues(secretSharingInformation, selfPubkey);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error in AddSharingInformationAsync.");
            }
        }

        private async Task CollectPiecesWithSecretSharingAsync(SecretSharingInformation secretSharingInformation,
            Hash newInValue, string selfPubkey)
        {
            var encryptedPieces = new Dictionary<string, byte[]>();
            var decryptedPieces = new Dictionary<string, byte[]>();

            var minersCount = secretSharingInformation.PreviousRound.RealTimeMinersInformation.Count;
            var minimumCount = minersCount.Mul(2).Div(3);
            var secretShares =
                SecretSharingHelper.EncodeSecret(newInValue.ToByteArray(), minimumCount, minersCount);
            
            foreach (var pair in secretSharingInformation.PreviousRound.RealTimeMinersInformation
                .OrderBy(m => m.Value.Order).ToDictionary(m => m.Key, m => m.Value.Order))
            {
                var pubkey = pair.Key;
                var order = pair.Value;

                var plainMessage = secretShares[order - 1];
                var receiverPublicKey = ByteArrayHelper.HexStringToByteArray(pubkey);
                var encryptedPiece = await _accountService.EncryptMessageAsync(receiverPublicKey, plainMessage);
                encryptedPieces[pubkey] = encryptedPiece;
                if (secretSharingInformation.PreviousRound.RealTimeMinersInformation.ContainsKey(selfPubkey) &&
                    secretSharingInformation.PreviousRound.RealTimeMinersInformation[selfPubkey].EncryptedPieces
                        .ContainsKey(pubkey))
                {
                    secretSharingInformation.PreviousRound.RealTimeMinersInformation[selfPubkey]
                            .EncryptedPieces[pubkey]
                        = ByteString.CopyFrom(encryptedPiece);
                }
                else
                {
                    continue;
                }

                if (!secretSharingInformation.PreviousRound.RealTimeMinersInformation.ContainsKey(pubkey)) continue;

                var encryptedShares =
                    secretSharingInformation.PreviousRound.RealTimeMinersInformation[pubkey].EncryptedPieces;
                if (!encryptedShares.Any() || !encryptedShares.ContainsKey(selfPubkey)) continue;
                var interestingMessage = encryptedShares[selfPubkey];
                var senderPublicKey = ByteArrayHelper.HexStringToByteArray(pubkey);

                var decryptedPiece =
                    await _accountService.DecryptMessageAsync(senderPublicKey, interestingMessage.ToByteArray());
                decryptedPieces[pubkey] = decryptedPiece;
                secretSharingInformation.PreviousRound.RealTimeMinersInformation[pubkey].DecryptedPieces[selfPubkey]
                    = ByteString.CopyFrom(decryptedPiece);
            }

            _encryptedPieces[secretSharingInformation.CurrentRoundId] = encryptedPieces;
            _decryptedPieces[secretSharingInformation.CurrentRoundId] = decryptedPieces;
        }

        private void RevealPreviousInValues(SecretSharingInformation secretSharingInformation, string selfPubkey)
        {
            var round = secretSharingInformation.PreviousRound;
            var minersCount = round.RealTimeMinersInformation.Count;
            var minimumCount = minersCount.Mul(2).Div(3);
            minimumCount = minimumCount == 0 ? 1 : minimumCount;

            var revealedInValues = new Dictionary<string, Hash>();

            foreach (var pair in round.RealTimeMinersInformation.OrderBy(m => m.Value.Order))
            {
                // Skip himself.
                if (pair.Key == selfPubkey) continue;

                var pubkey = pair.Key;
                var minerInRound = pair.Value;

                if (minerInRound.EncryptedPieces.Count < minimumCount) continue;
                if (minerInRound.DecryptedPieces.Count < minersCount) continue;

                // Reveal another miner's in value for target round:

                var orders = minerInRound.DecryptedPieces.Select((t, i) =>
                        round.RealTimeMinersInformation.Values
                            .First(m => m.Pubkey ==
                                        minerInRound.DecryptedPieces.Keys.ToList()[i]).Order)
                    .ToList();

                var sharedParts = minerInRound.DecryptedPieces.Values.ToList()
                    .Select(s => s.ToByteArray()).ToList();

                var revealedInValue =
                    HashHelper.ComputeFrom(SecretSharingHelper.DecodeSecret(sharedParts, orders, minimumCount));

                Logger.LogDebug($"Revealed in value of {pubkey} of round {round.RoundNumber}: {revealedInValue}");

                revealedInValues[pubkey] = revealedInValue;
            }

            _revealedInValues[secretSharingInformation.CurrentRoundId] = revealedInValues;
        }

        public Dictionary<string, byte[]> GetEncryptedPieces(long roundId)
        {
            _encryptedPieces.TryGetValue(roundId, out var encryptedPieces);
            Logger.LogDebug($"[GetEncryptedPieces]Round id: {roundId}");
            if (encryptedPieces != null)
            {
                Logger.LogDebug($"Encrypted/Shared {encryptedPieces.Count} pieces.");
            }

            _encryptedPieces.Remove(roundId);
            return encryptedPieces ?? new Dictionary<string, byte[]>();
        }

        public Dictionary<string, byte[]> GetDecryptedPieces(long roundId)
        {
            _decryptedPieces.TryGetValue(roundId, out var decryptedPieces);
            Logger.LogDebug($"[GetDecryptedPieces]Round id: {roundId}");
            if (decryptedPieces != null)
            {
                Logger.LogDebug($"Decrypted {decryptedPieces.Count} pieces for round of id {roundId}");
            }

            _decryptedPieces.Remove(roundId);
            return decryptedPieces ?? new Dictionary<string, byte[]>();
        }

        public Dictionary<string, Hash> GetRevealedInValues(long roundId)
        {
            _revealedInValues.TryGetValue(roundId, out var revealedInValues);
            Logger.LogDebug($"[GetRevealedInValues]Round id: {roundId}");
            if (revealedInValues != null)
            {
                Logger.LogDebug($"Revealed {revealedInValues.Count} in values for round of id {roundId}");
            }

            _revealedInValues.Remove(roundId);
            return revealedInValues ?? new Dictionary<string, Hash>();
        }

        private async Task<Hash> GenerateInValueAsync(IMessage message)
        {
            var data = HashHelper.ComputeFrom(message.ToByteArray());
            var bytes = await _accountService.SignAsync(data.ToByteArray());
            return HashHelper.ComputeFrom(bytes);
        }
    }
}