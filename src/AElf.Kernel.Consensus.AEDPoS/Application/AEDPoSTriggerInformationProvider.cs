using AElf.Kernel.Account.Application;
using Google.Protobuf;
using Volo.Abp.Threading;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSTriggerInformationProvider : ITriggerInformationProvider
    {
        private readonly IAccountService _accountService;
        private readonly IInValueCacheService _inValueCacheService;
        private readonly ISecretSharingService _secretSharingService;

        private ByteString Pubkey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

        public ILogger<AEDPoSTriggerInformationProvider> Logger { get; set; }

        public AEDPoSTriggerInformationProvider(IAccountService accountService,
            ISecretSharingService secretSharingService, IInValueCacheService inValueCacheService)
        {
            _accountService = accountService;
            _secretSharingService = secretSharingService;
            _inValueCacheService = inValueCacheService;
            
            Logger = NullLogger<AEDPoSTriggerInformationProvider>.Instance;
        }

        public BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes)
        {
            return new BytesValue {Value = Pubkey};
        }

        public BytesValue GetTriggerInformationForBlockHeaderExtraData(BytesValue consensusCommandBytes)
        {
            if (consensusCommandBytes == null)
            {
                return new AElfConsensusTriggerInformation
                {
                    Pubkey = Pubkey,
                    Behaviour = AElfConsensusBehaviour.UpdateValue
                }.ToBytesValue();
            }

            var command = consensusCommandBytes.ToConsensusCommand();
            var hint = command.Hint.ToAElfConsensusHint();

            if (hint.Behaviour == AElfConsensusBehaviour.UpdateValue)
            {
                var newInValue = _inValueCacheService.GetInValue(hint.RoundId);
                var previousInValue = _inValueCacheService.GetInValue(hint.PreviousRoundId);
                Logger.LogTrace($"New in value {newInValue} for round of id {hint.RoundId}");
                Logger.LogTrace($"Previous in value {previousInValue} for round of id {hint.PreviousRoundId}");
                var trigger = new AElfConsensusTriggerInformation
                {
                    Pubkey = Pubkey,
                    InValue = newInValue,
                    PreviousInValue = previousInValue,
                    Behaviour = hint.Behaviour
                };

                return trigger.ToBytesValue();
            }

            return new AElfConsensusTriggerInformation
            {
                Pubkey = Pubkey,
                Behaviour = hint.Behaviour
            }.ToBytesValue();
        }

        public BytesValue GetTriggerInformationForConsensusTransactions(BytesValue consensusCommandBytes)
        {
            if (consensusCommandBytes == null)
            {
                return new AElfConsensusTriggerInformation
                {
                    Pubkey = Pubkey,
                    Behaviour = AElfConsensusBehaviour.UpdateValue
                }.ToBytesValue();
            }

            var command = consensusCommandBytes.ToConsensusCommand();
            var hint = command.Hint.ToAElfConsensusHint();

            if (hint.Behaviour == AElfConsensusBehaviour.UpdateValue)
            {
                var inValue = _inValueCacheService.GetInValue(hint.RoundId);
                var trigger = new AElfConsensusTriggerInformation
                {
                    Pubkey = Pubkey,
                    InValue = inValue,
                    PreviousInValue = _inValueCacheService.GetInValue(hint.PreviousRoundId),
                    Behaviour = hint.Behaviour,
                };

                var secretPieces = _secretSharingService.GetEncryptedPieces(hint.RoundId);
                foreach (var secretPiece in secretPieces)
                {
                    trigger.EncryptedPieces.Add(secretPiece.Key, ByteString.CopyFrom(secretPiece.Value));
                }

                var decryptedPieces = _secretSharingService.GetDecryptedPieces(hint.RoundId);
                foreach (var decryptedPiece in decryptedPieces)
                {
                    trigger.DecryptedPieces.Add(decryptedPiece.Key, ByteString.CopyFrom(decryptedPiece.Value));
                }

                var revealedInValues = _secretSharingService.GetRevealedInValues(hint.RoundId);
                foreach (var revealedInValue in revealedInValues)
                {
                    trigger.RevealedInValues.Add(revealedInValue.Key, revealedInValue.Value);
                }

                return trigger.ToBytesValue();
            }

            return new AElfConsensusTriggerInformation
            {
                Pubkey = Pubkey,
                Behaviour = hint.Behaviour
            }.ToBytesValue();
        }
    }
}