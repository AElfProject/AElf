using System.Threading.Tasks;
using AElf.Sdk.CSharp;
using AElf.Types;
using Acs0;
using AElf.Kernel.SmartContract.Events;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Application
{
    public class CodeCheckRequiredLogEventProcessor : IBestChainFoundLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly ICodeCheckService _codeCheckService;
        private ILocalEventBus LocalEventBus { get; set; }

        private LogEvent _interestedEvent;

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address = _smartContractAddressService.GetZeroSmartContractAddress();

                _interestedEvent = new CodeCheckRequired().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public CodeCheckRequiredLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ICodeCheckService codeCheckService)
        {
            _smartContractAddressService = smartContractAddressService;

            _codeCheckService = codeCheckService;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            // a new task for time-consuming code check job 
            Task.Run(async () =>
            {
                var eventData = new CodeCheckRequired();
                eventData.MergeFrom(logEvent);
                var codeCheckResult = await _codeCheckService.PerformCodeCheckAsync(eventData.Code.ToByteArray(),
                    transactionResult.BlockHash, transactionResult.BlockNumber);
                if (!codeCheckResult)
                    return;

                await LocalEventBus.PublishAsync(new TransactionResultCheckedEvent
                {
                    TransactionResult = transactionResult
                });
            });
            return Task.CompletedTask;
        }
    }
}