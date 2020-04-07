using System.Collections.Generic;
using System.Threading.Tasks;
using Acs0;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class CodeUpdatedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
        private readonly ISmartContractRegistrationInStateProvider _smartContractRegistrationInStateProvider;
        private readonly ISmartContractExecutiveService _smartContractExecutiveService;

        private LogEvent _interestedEvent;

        public ILogger<CodeUpdatedLogEventProcessor> Logger { get; set; }

        public override LogEvent InterestedEvent
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
            ISmartContractRegistrationProvider smartContractRegistrationProvider,
            ISmartContractRegistrationInStateProvider smartContractRegistrationInStateProvider,
            ISmartContractExecutiveService smartContractExecutiveService)
        {
            _smartContractAddressService = smartContractAddressService;
            _smartContractRegistrationProvider = smartContractRegistrationProvider;
            _smartContractRegistrationInStateProvider = smartContractRegistrationInStateProvider;
            _smartContractExecutiveService = smartContractExecutiveService;

            Logger = NullLogger<CodeUpdatedLogEventProcessor>.Instance;
        }

        protected override async Task ProcessLogEventAsync(Block block, LogEvent logEvent)
        {
            var eventData = new CodeUpdated();
            eventData.MergeFrom(logEvent);

            var smartContractRegistration =
                await _smartContractRegistrationInStateProvider.GetSmartContractRegistrationAsync(
                    new ChainContext
                    {
                        BlockHash = block.GetHash(),
                        BlockHeight = block.Height
                    }, eventData.Address);
            await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, eventData.Address, smartContractRegistration);
            _smartContractExecutiveService.CleanExecutive(eventData.Address);
            Logger.LogDebug($"Updated contract {eventData}");
        }
    }
}