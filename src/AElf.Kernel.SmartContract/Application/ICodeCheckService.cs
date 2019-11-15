using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.ParliamentAuth;
using AElf.CSharp.CodeOps;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ICodeCheckService
    {
        void Enable();
        void Disable();

        Task PerformCodeCheckAsync(TransactionResult transactionResult, LogEvent logEvent);
    }

    public class CodeCheckService : ICodeCheckService, ISingletonDependency
    {
        private readonly IReadyToApproveProposalCacheProvider _readyToApproveProposalCacheProvider;
        
        private readonly ContractAuditor _contractAuditor = new ContractAuditor(null, null);
        
        public ILogger<CodeCheckService> Logger { get; set; }
        
        private volatile bool _isEnabled = false;

        public CodeCheckService(IReadyToApproveProposalCacheProvider readyToApproveProposalCacheProvider)
        {
            _readyToApproveProposalCacheProvider = readyToApproveProposalCacheProvider;
            
            Logger = NullLogger<CodeCheckService>.Instance;
        }

        public void Enable()
        {
            _isEnabled = true;
        }

        public void Disable()
        {
            _isEnabled = false;
        }

        public Task PerformCodeCheckAsync(TransactionResult transactionResult, LogEvent logEvent)
        {
            if (!_isEnabled)
                return Task.CompletedTask;
            
            var eventData = new CodeCheckRequired();
            eventData.MergeFrom(logEvent);
            
            try
            {
                // Approve proposal related to CodeCheckRequired event
                var proposalId = ProposalCreated.Parser
                    .ParseFrom(transactionResult.Logs.First(l => l.Name == nameof(ProposalCreated)).NonIndexed)
                    .ProposalId;
                
                // Check contract code
                _contractAuditor.Audit(eventData.Code.ToByteArray(), true);
                
                // Cache proposal id to generate system approval transaction later
                _readyToApproveProposalCacheProvider.TryCacheProposalToApprove(proposalId);
            }
            catch (InvalidCodeException e)
            {
                // May do something else to indicate that the contract has an issue
                Logger.LogError("Contract code did not pass audit.", e);
            }
            
            return Task.CompletedTask;
        }
    }
}