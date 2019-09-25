using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.WebApp.Application.Chain.Application
{
    internal class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly Address _consensusContractAddress;

        private readonly ContractEventDiscoveryService<MiningInformationUpdated>
            _miningInformationUpdatedEventDiscoveryService;

        private readonly IMiningSequenceService _miningSequenceService;

        public BestChainFoundEventHandler(SmartContractAddressService smartContractAddressService,
            ContractEventDiscoveryService<MiningInformationUpdated> miningInformationUpdatedEventDiscoveryService,
            IMiningSequenceService miningSequenceService)
        {
            _consensusContractAddress =
                smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            _miningInformationUpdatedEventDiscoveryService = miningInformationUpdatedEventDiscoveryService;
            _miningSequenceService = miningSequenceService;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            foreach (var executedBlockHash in eventData.ExecutedBlocks)
            {
                var miningInformationUpdated =
                    (await _miningInformationUpdatedEventDiscoveryService.GetEventMessagesAsync(executedBlockHash,
                        _consensusContractAddress)).FirstOrDefault();
                if (miningInformationUpdated != null)
                {
                    _miningSequenceService.AddMiningInformation(new MiningSequenceDto
                    {
                        Pubkey = miningInformationUpdated.Pubkey,
                        Behaviour = miningInformationUpdated.Behaviour.ToString(),
                        MiningTime = miningInformationUpdated.MiningTime,
                        BlockHeight = miningInformationUpdated.BlockHeight,
                        PreviousBlockHash = miningInformationUpdated.PreviousBlockHash
                    });
                }
            }
        }
    }
}