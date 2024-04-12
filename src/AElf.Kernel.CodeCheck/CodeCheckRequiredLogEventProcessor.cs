using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.CodeCheck.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;

namespace AElf.Kernel.CodeCheck;

public class CodeCheckRequiredLogEventProcessor : LogEventProcessorBase, IBlocksExecutionSucceededLogEventProcessor
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ICodeCheckJobProcessor _codeCheckJobProcessor;

    public CodeCheckRequiredLogEventProcessor(ISmartContractAddressService smartContractAddressService,
        ICodeCheckJobProcessor codeCheckJobProcessor)
    {
        _smartContractAddressService = smartContractAddressService;
        _codeCheckJobProcessor = codeCheckJobProcessor;

        Logger = NullLogger<CodeCheckRequiredLogEventProcessor>.Instance;
    }

    public ILogger<CodeCheckRequiredLogEventProcessor> Logger { get; set; }

    public override Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
    {
        if (InterestedEvent != null)
            return Task.FromResult(InterestedEvent);

        var address = _smartContractAddressService.GetZeroSmartContractAddress();
        if (address == null) return null;

        InterestedEvent = GetInterestedEvent<CodeCheckRequired>(address);

        return Task.FromResult(InterestedEvent);
    }

    public override async Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
    {
        Logger.LogInformation("Start handling CodeCheckRequired log event.");
        foreach (var events in logEventsMap)
        {
            var transactionResult = events.Key;
            foreach (var logEvent in events.Value)
            {
                var eventData = new CodeCheckRequired();
                eventData.MergeFrom(logEvent);

                var proposalId = ProposalCreated.Parser
                    .ParseFrom(transactionResult.Logs.First(l => l.Name == nameof(ProposalCreated)).NonIndexed)
                    .ProposalId;

                var code = eventData.Code.ToByteArray();
                var sendResult = await _codeCheckJobProcessor.SendAsync(new CodeCheckJob
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height,
                    ContractCode = code,
                    ContractCategory = eventData.Category,
                    IsSystemContract = eventData.IsSystemContract,
                    IsUserContract = eventData.IsUserContract,
                    CodeCheckProposalId = proposalId,
                    ProposedContractInputHash = eventData.ProposedContractInputHash
                });

                if (!sendResult)
                {
                    Logger.LogError(
                        "Unable to perform code check. BlockHash: {BlockHash}, BlockHeight: {BlockHeight}, CodeHash: {CodeHash}, ProposalId: {ProposalId}",
                        block.GetHash(), block.Height, HashHelper.ComputeFrom(code).ToHex(), proposalId.ToHex());
                }
            }
        }
    }
}