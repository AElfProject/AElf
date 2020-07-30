using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Miner
{
    internal class CleanBlockExecutedDataChangeHeightEventHandler : CleanBlockExecutedDataChangeHeightBaseEventHandler<
        Int32Value>, ILocalEventHandler<CleanBlockExecutedDataChangeHeightEventData>, ITransientDependency
    {
        public CleanBlockExecutedDataChangeHeightEventHandler(
            ICachedBlockchainExecutedDataService<Int32Value> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }
    }
}