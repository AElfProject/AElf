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

        public void RemoveForkCache(List<Hash> blockHashes)
        {
            _smartContractExecutiveProvider.RemoveForkCache(blockHashes);
        }

        public void SetIrreversedCache(List<Hash> blockHashes)
        {
            _smartContractExecutiveProvider.SetIrreversedCache(blockHashes);
        }

        public void SetIrreversedCache(Hash blockHash)
        {
            _smartContractExecutiveProvider.SetIrreversedCache(blockHash);
        }
    }
}