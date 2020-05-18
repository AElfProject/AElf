using AElf.Kernel.SmartContract.Events;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContract.Application
{
    public class CleanBlockExecutedDataChangeHeightEventHandler :
        CleanBlockExecutedDataChangeHeightBaseEventHandler<SmartContractRegistration>,
        ILocalEventHandler<CleanBlockExecutedDataChangeHeightEventData>, ITransientDependency
    {
        public CleanBlockExecutedDataChangeHeightEventHandler(
            ICachedBlockchainExecutedDataService<SmartContractRegistration> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }
    }
    
    public class CleanSmartContractAddressChangeHeightEventHandler :
        CleanBlockExecutedDataChangeHeightBaseEventHandler<SmartContractAddress>,
        ILocalEventHandler<CleanBlockExecutedDataChangeHeightEventData>, ITransientDependency
    {
        public CleanSmartContractAddressChangeHeightEventHandler(
            ICachedBlockchainExecutedDataService<SmartContractAddress> cachedBlockchainExecutedDataService) : base(
            cachedBlockchainExecutedDataService)
        {
        }
    }
}