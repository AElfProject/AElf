using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Acs0;
using AElf.Contracts.ParliamentAuth;
using AElf.CSharp.CodeOps;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{

    public class CodeCheckRequiredLogEventHandler : ILogEventHandler, ISingletonDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        
        private readonly IReadyToApproveProposalCacheProvider _readyToApproveProposalCacheProvider;

        private readonly ICodeCheckActivationService _codeCheckActivationService;
        
        private readonly ContractAuditor _contractAuditor = new ContractAuditor(null, null);

        private bool _active = false;

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
            IReadyToApproveProposalCacheProvider readyToApproveProposalCacheProvider,
            ICodeCheckActivationService codeCheckActivationService)
        {
            _smartContractAddressService = smartContractAddressService;

            _readyToApproveProposalCacheProvider = readyToApproveProposalCacheProvider;

            _codeCheckActivationService = codeCheckActivationService;

            Logger = NullLogger<CodeCheckRequiredLogEventHandler>.Instance;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            if (!_codeCheckActivationService.IsActive())
                return Task.CompletedTask;
            
            var eventData = new CodeCheckRequired();
            eventData.MergeFrom(logEvent);

            var _ = ProcessLogEventAsync(transactionResult, eventData);

            return Task.CompletedTask;
        }

        private async Task ProcessLogEventAsync(TransactionResult transactionResult, CodeCheckRequired eventData)
        {
            try
            {
                // Check contract code
                _contractAuditor.Audit(eventData.Code.ToByteArray(), true);

                // Approve proposal related to CodeCheckRequired event
                var proposalId = ProposalCreated.Parser
                    .ParseFrom(transactionResult.Logs.First(l => l.Name == nameof(ProposalCreated)).NonIndexed)
                    .ProposalId;

                _readyToApproveProposalCacheProvider.TryCacheProposalToApprove(proposalId);
            }
            catch (InvalidCodeException e)
            {
                // May do something else to indicate that the contract has an issue
                Logger.LogError("Contract code did not pass audit.", e);
            }
        }
    }
}