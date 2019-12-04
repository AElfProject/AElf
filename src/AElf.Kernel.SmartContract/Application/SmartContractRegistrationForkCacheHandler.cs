using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractRegistrationForkCacheHandler: IForkCacheHandler, ITransientDependency
    {
        private readonly ISmartContractExecutiveProvider _smartContractExecutiveProvider;

        public SmartContractRegistrationForkCacheHandler(ISmartContractExecutiveProvider smartContractExecutiveProvider)
        {
            _smartContractExecutiveProvider = smartContractExecutiveProvider;
        }

        public Task RemoveForkCacheAsync(List<BlockIndex> blockIndexes)
        {
            _smartContractExecutiveProvider.RemoveForkCache(blockIndexes);
            return Task.CompletedTask;
        }

        public Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            _smartContractExecutiveProvider.SetIrreversedCache(blockIndexes);
            return Task.CompletedTask;
        }
    }
}