using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Parallel.Application
{
    public class CleanBlockExecutedDataChangeHeightEventHandler :
        CleanBlockExecutedDataChangeHeightBaseEventHandler<NonparallelContractCode>,
        ILocalEventHandler<CleanBlockExecutedDataChangeHeightEventData>, ITransientDependency
    {
        public CleanBlockExecutedDataChangeHeightEventHandler(
            ICachedBlockchainExecutedDataService<NonparallelContractCode> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }
    }
}