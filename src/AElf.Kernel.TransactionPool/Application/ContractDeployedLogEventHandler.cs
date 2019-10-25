using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class ContractDeployedLogEventHandler : ILogEventHandler, ISingletonDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;

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
            IDeployedContractAddressProvider deployedContractAddressProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _deployedContractAddressProvider = deployedContractAddressProvider;

            Logger = NullLogger<ContractDeployedLogEventHandler>.Instance;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new ContractDeployed();
            eventData.MergeFrom(logEvent);

            _deployedContractAddressProvider.AddDeployedContractAddress(eventData.Address);
            Logger.LogTrace($"Added deployed contract address of {eventData}");

            return Task.CompletedTask;
        }
    }
}