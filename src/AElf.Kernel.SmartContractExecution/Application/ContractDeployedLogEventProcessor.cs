using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AElf.CSharp.Core.Extension;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class ContractDeployedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
        private readonly ISmartContractRegistrationInStateProvider _smartContractRegistrationInStateProvider;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        private LogEvent _interestedEvent;

        public ILogger<ContractDeployedLogEventProcessor> Logger { get; set; }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address = _smartContractAddressService.GetZeroSmartContractAddress();

                _interestedEvent = new ContractDeployed().ToLogEvent(address);

                return _interestedEvent;
            }
        }
        
        public ContractDeployedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ISmartContractRegistrationProvider smartContractRegistrationProvider,
            ISmartContractRegistrationInStateProvider smartContractRegistrationInStateProvider, 
            ISmartContractExecutiveService smartContractExecutiveService)
        {
            _smartContractAddressService = smartContractAddressService;
            _smartContractRegistrationProvider = smartContractRegistrationProvider;
            _smartContractRegistrationInStateProvider = smartContractRegistrationInStateProvider;
            _smartContractExecutiveService = smartContractExecutiveService;

            Logger = NullLogger<ContractDeployedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new ContractDeployed();
            eventData.MergeFrom(logEvent);

            var smartContractRegistration =
                await _smartContractRegistrationInStateProvider.GetSmartContractRegistrationAsync(new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                }, eventData.Address);

            await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, eventData.Address, smartContractRegistration);
            if (block.Height > Constants.GenesisBlockHeight)
                _smartContractExecutiveService.CleanExecutive(eventData.Address);
            Logger.LogDebug($"Deployed contract {eventData}");
        }
    }
}