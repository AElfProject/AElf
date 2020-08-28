using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Kernel.CodeCheck.Application;
using AElf.Kernel.Proposal.Application;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;

namespace AElf.Kernel.CodeCheck
{
    public class CodeCheckRequiredLogEventProcessor : LogEventProcessorBase, IBlocksExecutionSucceededLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ICodeCheckService _codeCheckService;
        private readonly IProposalService _proposalService;
        
        public CodeCheckRequiredLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ICodeCheckService codeCheckService, IProposalService proposalService)
        {
            _smartContractAddressService = smartContractAddressService;
            _codeCheckService = codeCheckService;
            _proposalService = proposalService;
        }

        public override Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
        {
            if (InterestedEvent != null)
                return Task.FromResult(InterestedEvent);

            var address = _smartContractAddressService.GetZeroSmartContractAddress();
            if (address == null) return null;
            
            InterestedEvent = GetInterestedEvent<CodeCheckRequired>(address);
            
            return Task.FromResult(InterestedEvent);
        }

        public override Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
        {
            foreach (var events in logEventsMap)
            {
                var transactionResult = events.Key;
                foreach (var logEvent in events.Value)
                {
                    // a new task for time-consuming code check job 
                    Task.Run(async () =>
                    {
                        var eventData = new CodeCheckRequired();
                        eventData.MergeFrom(logEvent);
                        var codeCheckResult = await _codeCheckService.PerformCodeCheckAsync(
                            eventData.Code.ToByteArray(),
                            transactionResult.BlockHash, transactionResult.BlockNumber, eventData.Category, eventData.IsSystemContract);
                        if (!codeCheckResult)
                            return;

                        var proposalId = ProposalCreated.Parser
                            .ParseFrom(transactionResult.Logs.First(l => l.Name == nameof(ProposalCreated)).NonIndexed)
                            .ProposalId;
                        // Cache proposal id to generate system approval transaction later
                        _proposalService.AddNotApprovedProposal(proposalId, transactionResult.BlockNumber);
                    });
                }
            }

            return Task.CompletedTask;
        }
    }
}