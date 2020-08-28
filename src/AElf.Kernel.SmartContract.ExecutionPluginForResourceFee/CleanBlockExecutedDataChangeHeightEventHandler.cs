using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    internal class CleanBlockExecutedDataChangeHeightEventHandler :
        CleanBlockExecutedDataChangeHeightBaseEventHandler<TotalResourceTokensMaps>,
        ILocalEventHandler<CleanBlockExecutedDataChangeHeightEventData>, ITransientDependency
    {
        public CleanBlockExecutedDataChangeHeightEventHandler(
            ICachedBlockchainExecutedDataService<TotalResourceTokensMaps> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }
    }
}