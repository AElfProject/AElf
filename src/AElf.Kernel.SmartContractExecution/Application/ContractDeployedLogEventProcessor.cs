using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class ContractDeployedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
        private readonly ISmartContractRegistrationInStateProvider _smartContractRegistrationInStateProvider;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        public ILogger<ContractDeployedLogEventProcessor> Logger { get; set; }

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
        
        public override Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
        {
            if (InterestedEvent != null)
                return Task.FromResult(InterestedEvent);

            var address = _smartContractAddressService.GetZeroSmartContractAddress();
            if (address == null) return null;
            
            InterestedEvent = GetInterestedEvent<ContractDeployed>(address);

            return Task.FromResult(InterestedEvent);
        }

        protected override async Task ProcessLogEventAsync(Block block, LogEvent logEvent)
        {
            var eventData = new ContractDeployed();
            eventData.MergeFrom(logEvent);

            var chainContext = new ChainContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            
            var smartContractRegistration =
                await _smartContractRegistrationInStateProvider.GetSmartContractRegistrationAsync(chainContext
                    , eventData.Address);

            await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext, eventData.Address,
                smartContractRegistration);
            if (block.Height > AElfConstants.GenesisBlockHeight)
                _smartContractExecutiveService.CleanExecutive(eventData.Address);

            if (eventData.Name != null)
                await _smartContractAddressService.SetSmartContractAddressAsync(chainContext, eventData.Name.ToStorageKey(),
                    eventData.Address);
            
            Logger.LogDebug($"Deployed contract {eventData}");
        }
    }
}