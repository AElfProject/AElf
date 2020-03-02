using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class ContractDeployedLogEventHandler : IBlockAcceptedLogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;
        private readonly ISmartContractRegistrationService _smartContractRegistrationService;

        private LogEvent _interestedEvent;

        public ILogger<ContractDeployedLogEventHandler> Logger { get; set; }

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

        public ContractDeployedLogEventHandler(ISmartContractAddressService smartContractAddressService,
            IDeployedContractAddressProvider deployedContractAddressProvider,
            ISmartContractRegistrationService smartContractRegistrationService)
        {
            _smartContractAddressService = smartContractAddressService;
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _smartContractRegistrationService = smartContractRegistrationService;

            Logger = NullLogger<ContractDeployedLogEventHandler>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new ContractDeployed();
            eventData.MergeFrom(logEvent);

            _deployedContractAddressProvider.AddDeployedContractAddress(eventData.Address,
                new BlockIndex {BlockHash = block.GetHash(), BlockHeight = block.Height});
            Logger.LogDebug($"Added deployed contract address of {eventData}");
            await _smartContractRegistrationService.AddSmartContractRegistrationAsync(eventData.Address, eventData.CodeHash,
                new BlockIndex
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                });
        }
    }
}