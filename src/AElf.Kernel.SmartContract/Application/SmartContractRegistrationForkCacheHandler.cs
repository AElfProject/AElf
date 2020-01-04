using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractRegistrationForkCacheHandler: IForkCacheHandler, ITransientDependency
    {
        private readonly ISmartContractRegistrationService _smartContractRegistrationService;
        private readonly ISmartContractCodeHistoryService _smartContractCodeHistoryService;

        public SmartContractRegistrationForkCacheHandler(ISmartContractRegistrationService smartContractRegistrationService, 
            ISmartContractCodeHistoryService smartContractCodeHistoryService)
        {
            _smartContractRegistrationService = smartContractRegistrationService;
            _smartContractCodeHistoryService = smartContractCodeHistoryService;
        }

        public async Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            await _smartContractRegistrationService.RemoveForkCacheAsync(blockIndexes);
            await _smartContractCodeHistoryService.RemoveAsync(blockIndexes);
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _smartContractRegistrationService.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}