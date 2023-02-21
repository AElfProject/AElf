using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.CodeCheck.Application;
using Volo.Abp.EventBus;

namespace AElf.Kernel.CodeCheck;

public class NewIrreversibleBlockFoundEventHandler : ILocalEventHandler<NewIrreversibleBlockFoundEvent>
    //, ITransientDependency // Not remove code hash list for now, hash list won't be so much.
{
    private readonly ICheckedCodeHashProvider _checkedCodeHashProvider;
    private readonly ICodeCheckProposalService _codeCheckProposalService;

    public NewIrreversibleBlockFoundEventHandler(ICheckedCodeHashProvider checkedCodeHashProvider,
        ICodeCheckProposalService codeCheckProposalService)
    {
        _checkedCodeHashProvider = checkedCodeHashProvider;
        _codeCheckProposalService = codeCheckProposalService;
    }

    public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
    {
        await _checkedCodeHashProvider.RemoveCodeHashAsync(new BlockIndex
        {
            BlockHash = eventData.BlockHash,
            BlockHeight = eventData.BlockHeight
        }, eventData.PreviousIrreversibleBlockHeight);

        await _codeCheckProposalService.ClearProposalByLibAsync(eventData.BlockHash, eventData.BlockHeight);
    }
}