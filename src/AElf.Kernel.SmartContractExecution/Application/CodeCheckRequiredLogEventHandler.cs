using System.Threading.Tasks;
using AElf.Sdk.CSharp;
using AElf.Types;
using Acs0;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class CodeCheckRequiredLogEventHandler : IBestChainFoundLogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        
        private readonly ICodeCheckService _codeCheckService;

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

        public CodeCheckRequiredLogEventHandler(ISmartContractAddressService smartContractAddressService,
            ICodeCheckService codeCheckService)
        {
            _smartContractAddressService = smartContractAddressService;
            
            _codeCheckService = codeCheckService;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            return Task.Run(() => _codeCheckService.PerformCodeCheckAsync(transactionResult, logEvent));
        }
    }
}