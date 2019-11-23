using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class MockContractRemarksManager : IContractRemarksManager
    {
        public static bool NonParallelizable = false;

        public async Task<ContractRemarks> GetContractRemarksAsync(IChainContext chainContext, Address address, Hash codeHash)
        {
            return await Task.FromResult(new ContractRemarks
            {
                CodeHash = codeHash,
                ContractAddress = address,
                NonParallelizable = NonParallelizable
            });
        }

        public async Task SetContractRemarksAsync(Address address, ContractRemarks contractRemarks)
        {
            throw new System.NotImplementedException();
        }

        public void AddCodeHashCache(IBlockIndex blockIndex, Address address, Hash codeHash)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveContractRemarksCache(List<Hash> blockHashes)
        {
            throw new System.NotImplementedException();
        }

        public async Task SetIrreversedCacheAsync(Hash blockHash)
        {
            throw new System.NotImplementedException();
        }
    }
}