using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class CodeUpdatedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISmartContractRegistrationService _smartContractRegistrationService;

        private LogEvent _interestedEvent;

        public ILogger<CodeUpdatedLogEventProcessor> Logger { get; set; }

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

        public CodeUpdatedLogEventProcessor(ISmartContractAddressService smartContractAddressService, 
            ISmartContractRegistrationService smartContractRegistrationService)
        {
            _smartContractAddressService = smartContractAddressService;
            _smartContractRegistrationService = smartContractRegistrationService;

            Logger = NullLogger<CodeUpdatedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new CodeUpdated();
            eventData.MergeFrom(logEvent);

            await _smartContractRegistrationService.AddSmartContractRegistrationAsync(eventData.Address, eventData.NewCodeHash,
                new BlockIndex
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                });
            Logger.LogDebug($"Updated contract {eventData}");
        }
    }
}