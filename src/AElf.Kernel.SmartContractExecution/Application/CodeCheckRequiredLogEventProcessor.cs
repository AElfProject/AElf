using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Acs0;
using Acs3;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForProposal;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class CodeCheckRequiredLogEventProcessor : IBestChainFoundLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        private readonly ICodeCheckService _codeCheckService;
        //TODO: smart contract executing should know nothing about proposal
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

        public CodeCheckRequiredLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ICodeCheckService codeCheckService, IProposalService proposalService)
        {
            _smartContractAddressService = smartContractAddressService;

            _codeCheckService = codeCheckService;
            _proposalService = proposalService;
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

                //TODO: smart contract executing should know nothing about proposal
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