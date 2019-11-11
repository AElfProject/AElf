using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Cryptography.SecretSharing;
using AElf.Kernel.Account.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class SecretSharingService : ISecretSharingService, ISingletonDependency
    {
        private readonly Dictionary<long, Dictionary<string, byte[]>> _encryptedPieces =
            new Dictionary<long, Dictionary<string, byte[]>>();

        private readonly Dictionary<long, Dictionary<string, byte[]>> _decryptedPieces =
            new Dictionary<long, Dictionary<string, byte[]>>();

        private readonly Dictionary<long, Dictionary<string, Hash>> _revealedInValues =
            new Dictionary<long, Dictionary<string, Hash>>();

        private readonly IInValueCacheService _inValueCacheService;
        private readonly IAccountService _accountService;

        public ILogger<SecretSharingService> Logger { get; set; }

        public SecretSharingService(IInValueCacheService inValueCacheService, IAccountService accountService)
        {
            _inValueCacheService = inValueCacheService;
            _accountService = accountService;

            Logger = NullLogger<SecretSharingService>.Instance;
        }

        public Task AddSharingInformationAsync(LogEvent logEvent)
        {
            try
            {
                var secretSharingInformation = new SecretSharingInformation();
                secretSharingInformation.MergeFrom(logEvent);

                var newInValue = GenerateInValue(secretSharingInformation);
                _inValueCacheService.AddInValue(secretSharingInformation.CurrentRoundId, newInValue);

                Logger.LogTrace(
                    $"Handling sharing information: {secretSharingInformation}. New in value: {newInValue}");

                var encryptedPieces = new Dictionary<string, byte[]>();
                var decryptedPieces = new Dictionary<string, byte[]>();

                var minersCount = secretSharingInformation.PreviousRound.RealTimeMinersInformation.Count;
                var minimumCount = minersCount.Mul(2).Div(3);
                var secretShares =
                    SecretSharingHelper.EncodeSecret(newInValue.ToByteArray(), minimumCount, minersCount);
                var selfPubkey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync).ToHex();
                foreach (var pair in secretSharingInformation.PreviousRound.RealTimeMinersInformation
                    .OrderBy(m => m.Value.Order).ToDictionary(m => m.Key, m => m.Value.Order))
                {
                    var pubkey = pair.Key;
                    var order = pair.Value;

                    var plainMessage = secretShares[order - 1];
                    var receiverPublicKey = ByteArrayHelper.HexStringToByteArray(pubkey);
                    var encryptedPiece = AsyncHelper.RunSync(() =>
                        _accountService.EncryptMessageAsync(receiverPublicKey, plainMessage));
                    encryptedPieces.Add(pubkey, encryptedPiece);
                    secretSharingInformation.PreviousRound.RealTimeMinersInformation[selfPubkey].EncryptedPieces
                        .Add(pubkey, ByteString.CopyFrom(encryptedPiece));

                    if (!secretSharingInformation.PreviousRound.RealTimeMinersInformation.ContainsKey(pubkey)) continue;

                    var encryptedShares =
                        secretSharingInformation.PreviousRound.RealTimeMinersInformation[pubkey].EncryptedPieces;
                    if (!encryptedShares.Any()) continue;
                    var interestingMessage = encryptedShares[selfPubkey];
                    var senderPublicKey = ByteArrayHelper.HexStringToByteArray(pubkey);

                    var decryptedPiece = AsyncHelper.RunSync(() =>
                        _accountService.DecryptMessageAsync(senderPublicKey, interestingMessage.ToByteArray()));
                    decryptedPieces[pubkey] = decryptedPiece;
                    secretSharingInformation.PreviousRound.RealTimeMinersInformation[pubkey].DecryptedPieces
                        .Add(selfPubkey, ByteString.CopyFrom(decryptedPiece));
                }

                _encryptedPieces[secretSharingInformation.CurrentRoundId] = encryptedPieces;
                _decryptedPieces[secretSharingInformation.CurrentRoundId] = decryptedPieces;

                RevealPreviousInValues(secretSharingInformation, selfPubkey);
                
                Logger.LogTrace($"Final secret sharing information: {secretSharingInformation}");

            }
            catch (Exception e)
            {
                Logger.LogError($"Error in AddSharingInformationAsync.\n{e.Message}\n{e.StackTrace}");
            }

            return Task.CompletedTask;
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
                    Hash.FromRawBytes(SecretSharingHelper.DecodeSecret(sharedParts, orders, minimumCount));

                Logger.LogDebug($"Revealed in value of {pubkey} of round {round.RoundNumber}: {revealedInValue}");

                revealedInValues.Add(pubkey, revealedInValue);
            }

            _revealedInValues.Add(secretSharingInformation.CurrentRoundId, revealedInValues);
        }

        public Dictionary<string, byte[]> GetEncryptedPieces(long roundId)
        {
            _encryptedPieces.TryGetValue(roundId, out var encryptedPieces);
            Logger.LogTrace($"[GetEncryptedPieces]Round id: {roundId}");
            if (encryptedPieces != null)
            {
                Logger.LogTrace($"Encrypted/Shared {encryptedPieces.Count} pieces.");
            }

            _encryptedPieces.Remove(roundId);
            return encryptedPieces ?? new Dictionary<string, byte[]>();
        }

        public Dictionary<string, byte[]> GetDecryptedPieces(long roundId)
        {
            _decryptedPieces.TryGetValue(roundId, out var decryptedPieces);
            Logger.LogTrace($"[GetDecryptedPieces]Round id: {roundId}");
            if (decryptedPieces != null)
            {
                Logger.LogTrace($"Decrypted {decryptedPieces.Count} pieces for round of id {roundId}");
            }

            _decryptedPieces.Remove(roundId);
            return decryptedPieces ?? new Dictionary<string, byte[]>();
        }

        public Dictionary<string, Hash> GetRevealedInValues(long roundId)
        {
            _revealedInValues.TryGetValue(roundId, out var revealedInValues);
            Logger.LogTrace($"[GetRevealedInValues]Round id: {roundId}");
            if (revealedInValues != null)
            {
                Logger.LogTrace($"Revealed {revealedInValues.Count} in values for round of id {roundId}");
            }

            _revealedInValues.Remove(roundId);
            return revealedInValues ?? new Dictionary<string, Hash>();
        }

        private Hash GenerateInValue(IMessage message)
        {
            var data = Hash.FromRawBytes(message.ToByteArray());
            var bytes = AsyncHelper.RunSync(() => _accountService.SignAsync(data.ToByteArray()));
            return Hash.FromRawBytes(bytes);
        }
    }
}