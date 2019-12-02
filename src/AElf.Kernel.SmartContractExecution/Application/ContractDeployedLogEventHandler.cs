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
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;

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
            ISmartContractExecutiveProvider smartContractRegistrationProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _deployedContractAddressProvider = deployedContractAddressProvider;
            _smartContractExecutiveProvider = smartContractRegistrationProvider;

            Logger = NullLogger<ContractDeployedLogEventHandler>.Instance;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new ContractDeployed();
            eventData.MergeFrom(logEvent);

            _deployedContractAddressProvider.AddDeployedContractAddress(eventData.Address,
                new BlockIndex {BlockHash = block.GetHash(), BlockHeight = block.Height});
            Logger.LogTrace($"Added deployed contract address of {eventData}");
            _smartContractExecutiveProvider.AddSmartContractRegistration(eventData.Address, eventData.CodeHash,
                new BlockIndex
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                });

            return Task.CompletedTask;
        }
    }
}