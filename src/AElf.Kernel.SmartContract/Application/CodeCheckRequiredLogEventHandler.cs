using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Acs0;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{

    public class CodeCheckRequiredLogEventHandler : ILogEventHandler, ISingletonDependency
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
            var _ = _codeCheckService.PerformCodeCheckAsync(transactionResult, logEvent);

            return Task.CompletedTask;
        }
    }
}