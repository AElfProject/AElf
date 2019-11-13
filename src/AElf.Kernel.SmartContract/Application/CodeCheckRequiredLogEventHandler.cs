using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Acs0;
using AElf.CSharp.CodeOps;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class CodeCheckRequiredLogEventHandler : ILogEventHandler, ISingletonDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        
        private readonly IReadyToApproveProposalCacheProvider _readyToApproveProposalCacheProvider;
        
        private readonly ContractAuditor _contractAuditor = new ContractAuditor(null, null);

        private LogEvent _interestedEvent;
        
        public ILogger<CodeCheckRequiredLogEventHandler> Logger { get; set; }
        
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
            IReadyToApproveProposalCacheProvider readyToApproveProposalCacheProvider)
        {
            _smartContractAddressService = smartContractAddressService;

            _readyToApproveProposalCacheProvider = readyToApproveProposalCacheProvider;

            Logger = NullLogger<CodeCheckRequiredLogEventHandler>.Instance;
        }
        
        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new CodeCheckRequired();
            eventData.MergeFrom(logEvent);

            // Check contract code
            _contractAuditor.Audit(eventData.Code.ToByteArray(), true);
            
            // Approve proposal related to CodeCheckRequired event
            var proposalId = Hash.Parser.ParseFrom(transactionResult.Logs[1].NonIndexed);
            
            _readyToApproveProposalCacheProvider.TryCacheProposalToApprove(proposalId);

            return Task.CompletedTask;
        }
    }
}