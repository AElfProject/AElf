using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal class CleanTotalTransactionFeesMapChangeHeightEventHandler :
        CleanBlockExecutedDataChangeHeightBaseEventHandler<TotalTransactionFeesMap>,
        ILocalEventHandler<CleanBlockExecutedDataChangeHeightEventData>, ITransientDependency
    {
        public CleanTotalTransactionFeesMapChangeHeightEventHandler(
            ICachedBlockchainExecutedDataService<TotalTransactionFeesMap> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }
    }
}