using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractRegistrationForkCacheHandler: IForkCacheHandler, ITransientDependency
    {
        private readonly ISmartContractRegistrationService _smartContractRegistrationService;
        private readonly IExecutiveService _executiveService;

        public SmartContractRegistrationForkCacheHandler(ISmartContractRegistrationService smartContractRegistrationService,
            IExecutiveService executiveService)
        {
            _smartContractRegistrationService = smartContractRegistrationService;
            _executiveService = executiveService;
        }

        public async Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            var codeHashDic = await _smartContractRegistrationService.RemoveForkCacheAsync(blockIndexes);
            var addresses = codeHashDic.Keys;
            foreach (var address in addresses)
            {
                _executiveService.ClearExecutives(address, codeHashDic[address]);
            }
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            var codeHashDic = _smartContractRegistrationService.SetIrreversedCache(blockIndexes);
            var addresses = codeHashDic.Keys;
            foreach (var address in addresses)
            {
                _executiveService.ClearExecutives(address, codeHashDic[address]);
            }
            return Task.CompletedTask;
        }
    }
}