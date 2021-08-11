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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.CodeCheck
{
    public class CodeCheckRequiredLogEventProcessor : LogEventProcessorBase, IBlocksExecutionSucceededLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ICodeCheckService _codeCheckService;
        private readonly IProposalService _proposalService;
        private readonly ICheckedCodeHashProvider _checkedCodeHashProvider;
        public ILogger<CodeCheckRequiredLogEventProcessor> Logger { get; set; }

        public CodeCheckRequiredLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ICodeCheckService codeCheckService, IProposalService proposalService,
            ICheckedCodeHashProvider checkedCodeHashProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _codeCheckService = codeCheckService;
            _proposalService = proposalService;
            _checkedCodeHashProvider = checkedCodeHashProvider;

            Logger = NullLogger<CodeCheckRequiredLogEventProcessor>.Instance;
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
            Logger.LogInformation("Start handling CodeCheckRequired log event.");
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
                            transactionResult.BlockHash, transactionResult.BlockNumber, eventData.Category,
                            eventData.IsSystemContract);
                        Logger.LogInformation($"Code check result: {codeCheckResult}");
                        if (!codeCheckResult)
                            return;

                        var proposalId = ProposalCreated.Parser
                            .ParseFrom(transactionResult.Logs.First(l => l.Name == nameof(ProposalCreated)).NonIndexed)
                            .ProposalId;
                        // Cache proposal id to generate system approval transaction later
                        _proposalService.AddNotApprovedProposal(proposalId, transactionResult.BlockNumber);

                        await _checkedCodeHashProvider.AddCodeHashAsync(new BlockIndex
                        {
                            BlockHash = block.GetHash(),
                            BlockHeight = block.Height
                        }, HashHelper.ComputeFrom(eventData.Code.ToByteArray()));
                    });
                }
            }

            return Task.CompletedTask;
        }
    }
}