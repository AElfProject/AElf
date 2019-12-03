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
        private readonly IProposalService _proposalService;
        
        private readonly ContractAuditor _contractAuditor = new ContractAuditor(null, null);
        
        public ILogger<CodeCheckService> Logger { get; set; }
        
        private volatile bool _isEnabled = false;

        public CodeCheckService(IProposalService proposalService)
        {
            _proposalService = proposalService;
            
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
            
            Logger.LogTrace($"Start code check.");
            
            var eventData = new CodeCheckRequired();
            eventData.MergeFrom(logEvent);
            
            try
            {
                // Check contract code
                _contractAuditor.Audit(eventData.Code.ToByteArray(), true);
                
                // Approve proposal related to CodeCheckRequired event
                var proposalId = ProposalCreated.Parser
                    .ParseFrom(transactionResult.Logs.First(l => l.Name == nameof(ProposalCreated)).NonIndexed)
                    .ProposalId;
                
                // Cache proposal id to generate system approval transaction later
                _proposalService.AddNotApprovedProposal(proposalId, transactionResult.BlockNumber);
            }
            catch (InvalidCodeException e)
            {
                // May do something else to indicate that the contract has an issue
                Logger.LogWarning(e.Message);
            }
            
            Logger.LogTrace($"Finish code check.");

            return Task.CompletedTask;
        }
    }
}