using System.Linq;
using System.Threading.Tasks;
using AElf.Sdk.CSharp;
using AElf.Types;
using Acs0;
using Acs3;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForProposal;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class CodeCheckRequiredLogEventHandler : IBestChainFoundLogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly ICodeCheckService _codeCheckService;
        private readonly IProposalService _proposalService;


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
            ICodeCheckService codeCheckService, IProposalService proposalService)
        {
            _smartContractAddressService = smartContractAddressService;

            _codeCheckService = codeCheckService;
            _proposalService = proposalService;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            // a new task for time-consuming code check job 
            Task.Run(async () =>
            {
                var eventData = new CodeCheckRequired();
                eventData.MergeFrom(logEvent);
                var codeCheckResult = await _codeCheckService.PerformCodeCheckAsync(eventData.Code.ToByteArray());
                if (!codeCheckResult)
                    return;

                var proposalId = ProposalCreated.Parser
                    .ParseFrom(transactionResult.Logs.First(l => l.Name == nameof(ProposalCreated)).NonIndexed)
                    .ProposalId;
                // Cache proposal id to generate system approval transaction later
                _proposalService.AddNotApprovedProposal(proposalId, transactionResult.BlockNumber);
            });
            
            return Task.CompletedTask;
        }
    }
}