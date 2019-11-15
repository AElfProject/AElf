using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.Miner.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class CodeUpdatedLogEventHandler: ILogEventHandler,ISingletonDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;

        private LogEvent _interestedEvent;

        public ILogger<CodeUpdatedLogEventHandler> Logger { get; set; }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address = _smartContractAddressService.GetZeroSmartContractAddress();

                _interestedEvent = new CodeUpdated().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public CodeUpdatedLogEventHandler(ISmartContractAddressService smartContractAddressService,
            ISmartContractExecutiveProvider smartContractRegistrationProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _smartContractExecutiveProvider = smartContractRegistrationProvider;

            Logger = NullLogger<CodeUpdatedLogEventHandler>.Instance;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new CodeUpdated();
            eventData.MergeFrom(logEvent);

            _smartContractExecutiveProvider.AddSmartContractRegistration(
                new BlockIndex {BlockHash = block.GetHash(), BlockHeight = block.Height}, eventData.Address,
                eventData.NewCodeHash);

            return Task.CompletedTask;
        }
    }
}