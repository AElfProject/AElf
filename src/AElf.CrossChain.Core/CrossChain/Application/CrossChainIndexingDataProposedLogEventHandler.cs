using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Sdk.CSharp;
using AElf.Types;

namespace AElf.CrossChain
{
    public class CrossChainIndexingDataProposedLogEventHandler : IBestChainFoundLogEventHandler
    {
        public LogEvent InterestedEvent => GetInterestedEvent();
        
        private readonly ISmartContractAddressService _smartContractAddressService;
        private LogEvent _interestedEvent;
        
        public CrossChainIndexingDataProposedLogEventHandler(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var crossChainIndexingDataProposedEvent = new CrossChainIndexingDataProposedEvent();
            crossChainIndexingDataProposedEvent.MergeFrom(logEvent);
            var crossChainBlockData = crossChainIndexingDataProposedEvent.ProposedCrossChainData;
        }

        private LogEvent GetInterestedEvent()
        {
            if (_interestedEvent != null)
                return _interestedEvent;

            var address =
                _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider
                    .Name);

            _interestedEvent = new CrossChainIndexingDataProposedEvent().ToLogEvent(address);

            return _interestedEvent;
        }
    }
}