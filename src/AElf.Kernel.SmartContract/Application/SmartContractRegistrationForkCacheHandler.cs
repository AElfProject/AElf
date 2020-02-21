using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    //TODO: remove
    public class SmartContractRegistrationForkCacheHandler: IForkCacheHandler, ITransientDependency
    {
        private readonly ISmartContractRegistrationService _smartContractRegistrationService;

        public SmartContractRegistrationForkCacheHandler(ISmartContractRegistrationService smartContractRegistrationService)
        {
            _smartContractRegistrationService = smartContractRegistrationService;
        }

        public async Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            await _smartContractRegistrationService.RemoveForkCacheAsync(blockIndexes);
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _smartContractRegistrationService.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}