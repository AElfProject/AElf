using System.Collections.Generic;
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

        public void RemoveForkCache(List<BlockIndex> blockIndexes)
        {
            _smartContractExecutiveProvider.RemoveForkCache(blockIndexes);
        }

        public void SetIrreversedCache(List<BlockIndex> blockIndexes)
        {
            _smartContractExecutiveProvider.SetIrreversedCache(blockIndexes);
        }
    }
}