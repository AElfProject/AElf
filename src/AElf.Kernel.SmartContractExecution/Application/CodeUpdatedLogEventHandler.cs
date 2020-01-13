using System.Threading.Tasks;
using Acs0;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class CodeUpdatedLogEventHandler : IBlockAcceptedLogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISmartContractRegistrationService _smartContractRegistrationService;

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
            ISmartContractRegistrationService smartContractRegistrationService)
        {
            _smartContractAddressService = smartContractAddressService;
            _smartContractRegistrationService = smartContractRegistrationService;

            Logger = NullLogger<CodeUpdatedLogEventHandler>.Instance;
        }

        public async Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
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