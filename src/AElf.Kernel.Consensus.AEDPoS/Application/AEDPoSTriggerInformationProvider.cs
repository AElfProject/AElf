using System.Collections.Generic;
using System.Threading.Tasks;
using Acs4;
using AElf.Kernel.Account.Application;
using Google.Protobuf;
using Volo.Abp.Threading;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf.WellKnownTypes;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class AEDPoSTriggerInformationProvider : ITriggerInformationProvider
    {
        private readonly IAccountService _accountService;
        private readonly IRandomHashCacheService _randomHashCacheService;
        private readonly IInValueCacheService _inValueCacheService;
        private readonly IBlockchainService _blockchainService;
        private readonly ISecretSharingService _secretSharingService;

        private ByteString Pubkey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

        public ILogger<AEDPoSTriggerInformationProvider> Logger { get; set; }

        public AEDPoSTriggerInformationProvider(IAccountService accountService,
            IRandomHashCacheService randomHashCacheService, IBlockchainService blockchainService,
            ISecretSharingService secretSharingService, IInValueCacheService inValueCacheService)
        {
            _accountService = accountService;
            _randomHashCacheService = randomHashCacheService;
            _blockchainService = blockchainService;
            _secretSharingService = secretSharingService;
            _inValueCacheService = inValueCacheService;
        }

        public BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes)
        {
            return new BytesValue {Value = Pubkey};
        }

        public async Task<BytesValue> GetTriggerInformationForBlockHeaderExtraDataAsync(
            BytesValue consensusCommandBytes)
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

        public async Task<BytesValue> GetTriggerInformationForConsensusTransactionsAsync(
            BytesValue consensusCommandBytes)
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

                var secretPieces = _secretSharingService.GetSharingPieces(hint.RoundId);
                foreach (var secretPiece in secretPieces)
                {
                    trigger.EncryptedShares.Add(secretPiece.Key, ByteString.CopyFrom(secretPiece.Value));
                }

                var revealedInValues = _secretSharingService.GetRevealedInValues(hint.RoundId);
                foreach (var revealedInValue in revealedInValues)
                {
                    trigger.RevealedInValues.Add(revealedInValue.Key, ByteString.CopyFrom(revealedInValue.Value));
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