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
        private readonly IBlockchainService _blockchainService;
        private readonly ISecretSharingService _secretSharingService;

        private ByteString PublicKey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

        public ILogger<AEDPoSTriggerInformationProvider> Logger { get; set; }

        public AEDPoSTriggerInformationProvider(IAccountService accountService,
            IRandomHashCacheService randomHashCacheService, IBlockchainService blockchainService,
            ISecretSharingService secretSharingService)
        {
            _accountService = accountService;
            _randomHashCacheService = randomHashCacheService;
            _blockchainService = blockchainService;
            _secretSharingService = secretSharingService;
        }

        public BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes)
        {
            return new BytesValue {Value = PublicKey};
        }

        public async Task<BytesValue> GetTriggerInformationForBlockHeaderExtraDataAsync(
            BytesValue consensusCommandBytes)
        {
            if (consensusCommandBytes == null)
            {
                return new AElfConsensusTriggerInformation
                {
                    Pubkey = PublicKey,
                    Behaviour = AElfConsensusBehaviour.UpdateValue
                }.ToBytesValue();
            }

            var command = consensusCommandBytes.ToConsensusCommand();
            var behaviour = command.Hint.ToAElfConsensusHint().Behaviour;

            if (behaviour == AElfConsensusBehaviour.UpdateValue)
            {
                var bestChainLastBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
                var bestChainLastBlockHash = bestChainLastBlockHeader.GetHash();

                _randomHashCacheService.SetGeneratedBlockBestChainHash(bestChainLastBlockHash,
                    bestChainLastBlockHeader.Height);

                var newRandomHash = GetRandomHash(command);
                _randomHashCacheService.SetRandomHash(bestChainLastBlockHash, newRandomHash);

                var information = new AElfConsensusTriggerInformation
                {
                    Pubkey = PublicKey,
                    RandomHash = newRandomHash,
                    PreviousRandomHash = _randomHashCacheService.GetLatestGeneratedBlockRandomHash(),
                    Behaviour = behaviour
                };

                return information.ToBytesValue();
            }

            return new AElfConsensusTriggerInformation
            {
                Pubkey = PublicKey,
                Behaviour = behaviour
            }.ToBytesValue();
        }

        public async Task<BytesValue> GetTriggerInformationForConsensusTransactionsAsync(
            BytesValue consensusCommandBytes)
        {
            if (consensusCommandBytes == null)
            {
                return new AElfConsensusTriggerInformation
                {
                    Pubkey = PublicKey,
                    Behaviour = AElfConsensusBehaviour.UpdateValue
                }.ToBytesValue();
            }

            var command = consensusCommandBytes.ToConsensusCommand();
            var hint = command.Hint.ToAElfConsensusHint();
            var bestChainLastBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();

            if (hint.Behaviour == AElfConsensusBehaviour.UpdateValue)
            {
                var trigger = new AElfConsensusTriggerInformation
                {
                    Pubkey = PublicKey,
                    RandomHash = _randomHashCacheService.GetRandomHash(bestChainLastBlockHeader.GetHash()),
                    PreviousRandomHash = _randomHashCacheService.GetLatestGeneratedBlockRandomHash(),
                    Behaviour = hint.Behaviour,
                };

                var secretPieces = _secretSharingService.GetSharingPieces(hint.RoundId);
                foreach (var secretPiece in secretPieces)
                {
                    trigger.Secrets.Add(secretPiece.Key, ByteString.CopyFrom(secretPiece.Value));
                }

                var revealedInValues = _secretSharingService.GetRevealedInValues(hint.RoundId);
                foreach (var revealedInValue in revealedInValues)
                {
                    trigger.Secrets.Add(revealedInValue.Key, ByteString.CopyFrom(revealedInValue.Value));
                }

                return trigger.ToBytesValue();
            }

            return new AElfConsensusTriggerInformation
            {
                Pubkey = PublicKey,
                Behaviour = hint.Behaviour
            }.ToBytesValue();
        }

        /// <summary>
        /// For generating in_value.
        /// </summary>
        /// <returns></returns>
        private Hash GetRandomHash(ConsensusCommand consensusCommand)
        {
            var data = Hash.FromRawBytes(consensusCommand.ArrangedMiningTime.ToByteArray());
            var bytes = AsyncHelper.RunSync(() => _accountService.SignAsync(data.ToByteArray()));
            return Hash.FromRawBytes(bytes);
        }
    }
}