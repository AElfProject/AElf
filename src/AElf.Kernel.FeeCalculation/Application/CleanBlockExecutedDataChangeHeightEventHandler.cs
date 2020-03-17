using System.Collections.Generic;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.FeeCalculation.Application
{
    public class CleanBlockExecutedDataChangeHeightEventHandler : CleanBlockExecutedDataChangeHeightBaseEventHandler<
            Dictionary<string, CalculateFunction>>, ILocalEventHandler<CleanBlockExecutedDataChangeHeightEventData>,
        ITransientDependency
    {
        public CleanBlockExecutedDataChangeHeightEventHandler(
            ICachedBlockchainExecutedDataService<Dictionary<string, CalculateFunction>>
                cachedBlockchainExecutedDataService) : base(cachedBlockchainExecutedDataService)
        {
        }
    }
}