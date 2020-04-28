using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.CrossChain;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Proposal.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.CrossChain.Indexing.Application
{
    public class CrossChainIndexingDataProposedLogEventProcessor : LogEventProcessorBase, IBlocksExecutionSucceededLogEventProcessor
    {
        public IOptionsMonitor<CrossChainConfigOptions> CrossChainConfigOptions { get; set; }
        public ILogger<CrossChainIndexingDataProposedLogEventProcessor> Logger { get; set; }

        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ICrossChainIndexingDataValidationService _crossChainIndexingDataValidationService;
        private readonly IProposalService _proposalService;

        public CrossChainIndexingDataProposedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ICrossChainIndexingDataValidationService crossChainIndexingDataValidationService,
            IProposalService proposalService)
        {
            _smartContractAddressService = smartContractAddressService;
            _crossChainIndexingDataValidationService = crossChainIndexingDataValidationService;
            _proposalService = proposalService;
        }
        
        public override async Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
        {
            if (InterestedEvent != null)
                return InterestedEvent;

            var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
                chainContext, CrossChainSmartContractAddressNameProvider.StringName);
            if (smartContractAddressDto == null) return null;
            
            var interestedEvent = GetInterestedEvent<CrossChainIndexingDataProposedEvent>(smartContractAddressDto
                .SmartContractAddress.Address);
            if (!smartContractAddressDto.Irreversible) return interestedEvent;
            InterestedEvent = interestedEvent;
            
            return InterestedEvent;
        }

        public override async Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
        {
            foreach (var events in logEventsMap)
            {
                var transactionResult = events.Key;
                foreach (var logEvent in events.Value)
                {
                    if (CrossChainConfigOptions.CurrentValue.CrossChainDataValidationIgnored)
                    {
                        Logger.LogTrace("Cross chain data validation disabled.");
                        return;
                    }

                    var crossChainIndexingDataProposedEvent = new CrossChainIndexingDataProposedEvent();
                    crossChainIndexingDataProposedEvent.MergeFrom(logEvent);
                    var crossChainBlockData = crossChainIndexingDataProposedEvent.ProposedCrossChainData;
                    if (crossChainBlockData.IsNullOrEmpty())
                        return;
                    var validationResult =
                        await _crossChainIndexingDataValidationService.ValidateCrossChainIndexingDataAsync(
                            crossChainBlockData,
                            block.GetHash(), block.Height);
                    if (validationResult)
                    {
                        Logger.LogDebug(
                            $"Valid cross chain indexing proposal found, block height {block.Height}, block hash {block.GetHash()} ");
                        var proposalId = ProposalCreated.Parser
                            .ParseFrom(transactionResult.Logs.First(l => l.Name == nameof(ProposalCreated)).NonIndexed)
                            .ProposalId;
                        _proposalService.AddNotApprovedProposal(proposalId, block.Height);
                    }
                }
            }
        }
    }
}