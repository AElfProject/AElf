using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using AElf.WebApp.Application.Chain.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.Application.Chain.Application
{
    public class MiningInformationUpdatedLogEventHandler : IBestChainFoundLogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IMiningSequenceRepository _miningSequenceRepository;

        private LogEvent _interestedEvent;
        
        private const int KeepRecordsCount = 256;

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
            IMiningSequenceRepository miningSequenceRepository)
        {
            _smartContractAddressService = smartContractAddressService;
            _miningSequenceRepository = miningSequenceRepository;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new MiningInformationUpdated();
            eventData.MergeFrom(logEvent);

            var miningSequenceDto = new MiningSequenceDto
            {
                Pubkey = eventData.Pubkey,
                Behaviour = eventData.Behaviour,
                MiningTime = eventData.MiningTime,
                BlockHeight = eventData.BlockHeight,
                PreviousBlockHash = eventData.PreviousBlockHash.ToHex()
            };

            _miningSequenceRepository.AddMiningSequence(miningSequenceDto);
            _miningSequenceRepository.ClearMiningSequences(KeepRecordsCount);

            return Task.CompletedTask;
        }
    }
}