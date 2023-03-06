using System.Collections.Generic;
using System.Linq;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.CodeCheck.Application;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.Proposal.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using Google.Protobuf;

namespace AElf.Kernel.CodeCheck;

public class CodeCheckRequiredLogEventProcessor : LogEventProcessorBase, IBlocksExecutionSucceededLogEventProcessor
{
    private readonly ICheckedCodeHashProvider _checkedCodeHashProvider;
    private readonly ICodeCheckService _codeCheckService;
    private readonly IProposalService _proposalService;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ISmartContractCodeService _smartContractCodeService;
    private readonly ICodeCheckProposalService _codeCheckProposalService;
    private readonly ICodePatchService _codePatchService;

    public CodeCheckRequiredLogEventProcessor(ISmartContractAddressService smartContractAddressService,
        ICodeCheckService codeCheckService, IProposalService proposalService,
        ICheckedCodeHashProvider checkedCodeHashProvider,
        ISmartContractCodeService smartContractCodeService, ICodeCheckProposalService codeCheckProposalService,
        ICodePatchService codePatchService)
    {
        _smartContractAddressService = smartContractAddressService;
        _codeCheckService = codeCheckService;
        _proposalService = proposalService;
        _checkedCodeHashProvider = checkedCodeHashProvider;
        _smartContractCodeService = smartContractCodeService;
        _codeCheckProposalService = codeCheckProposalService;
        _codePatchService = codePatchService;

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

    public override Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
    {
        Logger.LogInformation("Start handling CodeCheckRequired log event.");
        foreach (var events in logEventsMap)
        {
            var transactionResult = events.Key;
            foreach (var logEvent in events.Value)
                // a new task for time-consuming code check job 
                Task.Run(async () =>
                {
                    var eventData = new CodeCheckRequired();
                    eventData.MergeFrom(logEvent);

                    var code = eventData.Code.ToByteArray();
                    var codeHash = HashHelper.ComputeFrom(code);
                    if (eventData.IsUserContract &&
                        !_codePatchService.PerformCodePatch(code, eventData.Category, false, out code))
                    {
                        return;
                    }

                    var codeCheckResult = await _codeCheckService.PerformCodeCheckAsync(
                        code,
                        transactionResult.BlockHash, transactionResult.BlockNumber, eventData.Category,
                        eventData.IsSystemContract, eventData.IsUserContract);
                    Logger.LogInformation("Code check result: {codeCheckResult}, code hash: {codeHash}",
                        codeCheckResult, codeHash.ToHex());
                    if (!codeCheckResult)
                        return;
                    
                    var proposalId = ProposalCreated.Parser
                        .ParseFrom(transactionResult.Logs.First(l => l.Name == nameof(ProposalCreated)).NonIndexed)
                        .ProposalId;
                    
                    if (eventData.IsUserContract)
                    {
                        await _smartContractCodeService.AddSmartContractCodeAsync(codeHash, ByteString.CopyFrom(code));
                        _codeCheckProposalService.AddToReleasedProposal(proposalId, eventData.ProposedContractInputHash,
                            transactionResult.BlockNumber);
                    }

                    // Cache proposal id to generate system approval transaction later
                    _proposalService.AddNotApprovedProposal(proposalId, transactionResult.BlockNumber);

                    await _checkedCodeHashProvider.AddCodeHashAsync(new BlockIndex
                    {
                        BlockHash = block.GetHash(),
                        BlockHeight = block.Height
                    }, codeHash);
                });
        }

        return Task.CompletedTask;
    }
}