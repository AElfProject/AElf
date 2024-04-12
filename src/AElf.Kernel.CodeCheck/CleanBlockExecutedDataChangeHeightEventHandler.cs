using AElf.Contracts.Parliament;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using Volo.Abp.EventBus;

namespace AElf.Kernel.CodeCheck;

internal class CleanBlockExecutedDataChangeHeightEventHandler :
    CleanBlockExecutedDataChangeHeightBaseEventHandler<ProposalIdList>,
    ILocalEventHandler<CleanBlockExecutedDataChangeHeightEventData>, ITransientDependency
{
    public CleanBlockExecutedDataChangeHeightEventHandler(
        ICachedBlockchainExecutedDataService<ProposalIdList> cachedBlockchainExecutedDataService) : base(
        cachedBlockchainExecutedDataService)
    {
    }
}