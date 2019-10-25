using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.Application.Chain.Application
{
    public class MiningInformationUpdatedLogEventHandler : ILogEventHandler, ISingletonDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IMiningSequenceService _miningSequenceService;

        private LogEvent _interestedEvent;

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null) 
                    return _interestedEvent;
                
                var address =
                    _smartContractAddressService.GetAddressByContractName(
                        ConsensusSmartContractAddressNameProvider.Name);

                _interestedEvent = new MiningInformationUpdated().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public MiningInformationUpdatedLogEventHandler(ISmartContractAddressService smartContractAddressService,
            IMiningSequenceService miningSequenceService)
        {
            _smartContractAddressService = smartContractAddressService;
            _miningSequenceService = miningSequenceService;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new MiningInformationUpdated();
            eventData.MergeFrom(logEvent);

            _miningSequenceService.AddMiningInformation(new MiningSequenceDto
            {
                Pubkey = eventData.Pubkey,
                Behaviour = eventData.Behaviour,
                MiningTime = eventData.MiningTime,
                BlockHeight = eventData.BlockHeight,
                PreviousBlockHash = eventData.PreviousBlockHash
            });

            return Task.CompletedTask;
        }
    }
}