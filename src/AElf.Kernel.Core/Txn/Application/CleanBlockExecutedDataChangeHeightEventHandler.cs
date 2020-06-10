using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using Google.Protobuf.WellKnownTypes;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Txn.Application
{
    public class CleanBlockExecutedDataChangeHeightEventHandler : CleanBlockExecutedDataChangeHeightBaseEventHandler<
        BoolValue>, ILocalEventHandler<CleanBlockExecutedDataChangeHeightEventData>, ITransientDependency
    {
        public CleanBlockExecutedDataChangeHeightEventHandler(
            ICachedBlockchainExecutedDataService<BoolValue> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }
    }
}