using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using AElf.WebApp.Application.Chain.Infrastructure;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.WebApp.Application.Chain.Application
{
    internal class BestChainFoundEventHandler : ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private const int KeepRecordsCount = 256;

        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly ContractEventDiscoveryService<MiningInformationUpdated>
            _miningInformationUpdatedEventDiscoveryService;

        private readonly IMiningSequenceRepository _miningSequenceRepository;

        public BestChainFoundEventHandler(ISmartContractAddressService smartContractAddressService,
            ContractEventDiscoveryService<MiningInformationUpdated> miningInformationUpdatedEventDiscoveryService,
            IMiningSequenceRepository miningSequenceRepository)
        {
            _smartContractAddressService = smartContractAddressService;
            _miningInformationUpdatedEventDiscoveryService = miningInformationUpdatedEventDiscoveryService;
            _miningSequenceRepository = miningSequenceRepository;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            var consensusContractAddress = _smartContractAddressService.GetAddressByContractName(
                ConsensusSmartContractAddressNameProvider.Name);

            foreach (var executedBlockHash in eventData.ExecutedBlocks)
            {
                var miningInformationUpdated =
                    (await _miningInformationUpdatedEventDiscoveryService.GetEventMessagesAsync(executedBlockHash,
                        consensusContractAddress)).FirstOrDefault();
                if (miningInformationUpdated != null)
                {
                    var miningSequenceDto = new MiningSequenceDto
                    {
                        Pubkey = miningInformationUpdated.Pubkey,
                        Behaviour = miningInformationUpdated.Behaviour,
                        MiningTime = miningInformationUpdated.MiningTime,
                        BlockHeight = miningInformationUpdated.BlockHeight,
                        PreviousBlockHash = miningInformationUpdated.PreviousBlockHash.ToHex()
                    };

                    _miningSequenceRepository.AddMiningSequence(miningSequenceDto);
                    _miningSequenceRepository.ClearMiningSequences(KeepRecordsCount);
                }
            }
        }
    }
}