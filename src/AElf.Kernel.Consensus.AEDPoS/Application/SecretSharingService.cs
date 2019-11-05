using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Cryptography.SecretSharing;
using AElf.Kernel.Account.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Threading;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class SecretSharingService : ISecretSharingService, ISingletonDependency
    {
        private readonly Dictionary<long, Dictionary<string, byte[]>> _sharingPieces =
            new Dictionary<long, Dictionary<string, byte[]>>();

        private readonly Dictionary<long, Dictionary<string, byte[]>> _revealedInValues =
            new Dictionary<long, Dictionary<string, byte[]>>();

        private readonly IInValueCacheService _inValueCacheService;
        private readonly IAccountService _accountService;

        public SecretSharingService(IInValueCacheService inValueCacheService, IAccountService accountService)
        {
            _inValueCacheService = inValueCacheService;
            _accountService = accountService;
        }

        public Task AddSharingInformationAsync(SecretSharingInformation secretSharingInformation)
        {
            var newInValue = GenerateInValue(secretSharingInformation);
            _inValueCacheService.AddInValue(secretSharingInformation.CurrentRoundId, newInValue);

            var decryptedSecretShares = new Dictionary<string, byte[]>();
            var revealedSecretShares = new Dictionary<string, byte[]>();

            var minersCount = secretSharingInformation.PreviousRound.RealTimeMinersInformation.Count;
            var minimumCount = minersCount.Mul(2).Div(3);
            var secretShares = SecretSharingHelper.EncodeSecret(newInValue.ToByteArray(), minimumCount, minersCount);
            var selfPubkey = AsyncHelper.RunSync(_accountService.GetPublicKeyAsync);
            foreach (var pair in secretSharingInformation.PreviousRound.RealTimeMinersInformation
                .OrderBy(m => m.Value.Order).ToDictionary(m => m.Key, m => m.Value.Order))
            {
                var pubkey = pair.Key;
                var order = pair.Value;

                var plainMessage = secretShares[order - 1];
                var receiverPublicKey = ByteArrayHelper.HexStringToByteArray(pubkey);
                var encryptedInValue = AsyncHelper.RunSync(() =>
                    _accountService.EncryptMessageAsync(receiverPublicKey, plainMessage));
                decryptedSecretShares.Add(pubkey, encryptedInValue);

                if (!secretSharingInformation.PreviousRound.RealTimeMinersInformation.ContainsKey(pubkey)) continue;

                var encryptedShares =
                    secretSharingInformation.PreviousRound.RealTimeMinersInformation[pubkey].EncryptedInValues;
                if (!encryptedShares.Any()) continue;
                var interestingMessage = encryptedShares[selfPubkey.ToHex()];
                var senderPublicKey = ByteArrayHelper.HexStringToByteArray(pubkey);

                var decryptedInValue = AsyncHelper.RunSync(() =>
                    _accountService.DecryptMessageAsync(senderPublicKey, interestingMessage.ToByteArray()));
                revealedSecretShares[pubkey] = decryptedInValue;
            }

            _sharingPieces[secretSharingInformation.CurrentRoundId] = decryptedSecretShares;
            _revealedInValues[secretSharingInformation.CurrentRoundId] = revealedSecretShares;

            return Task.CompletedTask;
        }

        public Dictionary<string, byte[]> GetSharingPieces(long roundId)
        {
            _sharingPieces.TryGetValue(roundId, out var sharingPieces);
            _sharingPieces.Remove(roundId);
            return sharingPieces ?? new Dictionary<string, byte[]>();
        }

        public Dictionary<string, byte[]> GetRevealedInValues(long roundId)
        {
            _revealedInValues.TryGetValue(roundId, out var revealedInValues);
            _revealedInValues.Remove(roundId);
            return revealedInValues ?? new Dictionary<string, byte[]>();
        }

        private Hash GenerateInValue(IMessage message)
        {
            var data = Hash.FromRawBytes(message.ToByteArray());
            var bytes = AsyncHelper.RunSync(() => _accountService.SignAsync(data.ToByteArray()));
            return Hash.FromRawBytes(bytes);
        }
    }
}