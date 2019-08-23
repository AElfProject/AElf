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

        private ByteString PublicKey => ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync));

        public ILogger<AEDPoSTriggerInformationProvider> Logger { get; set; }

        public AEDPoSTriggerInformationProvider(IAccountService accountService,
            IRandomHashCacheService randomHashCacheService, IBlockchainService blockchainService)
        {
            _accountService = accountService;
            _randomHashCacheService = randomHashCacheService;
            _blockchainService = blockchainService;
        }

        public BytesValue GetTriggerInformationForConsensusCommand(BytesValue consensusCommandBytes)
        {
            return new BytesValue {Value = PublicKey};
        }

        public async Task<BytesValue> GetTriggerInformationForBlockHeaderExtraDataAsync(BytesValue consensusCommandBytes)
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
 
            if (behaviour == AElfConsensusBehaviour.UpdateValue ||
                behaviour == AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue)
            {
                var bestChainLastBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
                var bestChainLastBlockHash = bestChainLastBlockHeader.GetHash();

                _randomHashCacheService.SetGeneratedBlockPreviousBlockInformation(bestChainLastBlockHash,
                    bestChainLastBlockHeader.Height);
                
                var newRandomHash = GetRandomHash(command);
                _randomHashCacheService.SetRandomHash(bestChainLastBlockHash, newRandomHash);

                return new AElfConsensusTriggerInformation
                {
                    Pubkey = PublicKey,
                    RandomHash = newRandomHash,
                    PreviousRandomHash = _randomHashCacheService.GetLatestGeneratedBlockRandomHash(),
                    Behaviour = behaviour
                }.ToBytesValue();
            }

            return new AElfConsensusTriggerInformation
            {
                Pubkey = PublicKey,
                Behaviour = behaviour
            }.ToBytesValue();
        }

        public async Task<BytesValue> GetTriggerInformationForConsensusTransactionsAsync(BytesValue consensusCommandBytes)
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
            var bestChainLastBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();

            if (behaviour == AElfConsensusBehaviour.UpdateValue ||
                behaviour == AElfConsensusBehaviour.UpdateValueWithoutPreviousInValue)
            {
                var trigger = new AElfConsensusTriggerInformation
                {
                    Pubkey = PublicKey,
                    RandomHash = _randomHashCacheService.GetRandomHash(bestChainLastBlockHeader.GetHash()),
                    PreviousRandomHash = _randomHashCacheService.GetLatestGeneratedBlockRandomHash(),
                    Behaviour = behaviour
                };

                return trigger.ToBytesValue();
            }

            return new AElfConsensusTriggerInformation
            {
                Pubkey = PublicKey,
                Behaviour = behaviour
            }.ToBytesValue();
        }

        /// <summary>
        /// For generating in_value.
        /// </summary>
        /// <returns></returns>
        private Hash GetRandomHash(ConsensusCommand consensusCommand)
        {
            var data = Hash.FromRawBytes(consensusCommand.NextBlockMiningLeftMilliseconds
                .DumpByteArray());
            var bytes = AsyncHelper.RunSync(() => _accountService.SignAsync(data.ToByteArray()));
            return Hash.FromRawBytes(bytes);
        }
    }
}