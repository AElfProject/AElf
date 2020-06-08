using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Configuration
{
    internal class CleanBlockExecutedDataChangeHeightEventHandler : CleanBlockExecutedDataChangeHeightBaseEventHandler<
        BlockTransactionLimit>, ILocalEventHandler<CleanBlockExecutedDataChangeHeightEventData>, ITransientDependency
    {
        public CleanBlockExecutedDataChangeHeightEventHandler(
            ICachedBlockchainExecutedDataService<BlockTransactionLimit> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }
    }
}