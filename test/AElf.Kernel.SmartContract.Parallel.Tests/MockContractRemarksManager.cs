using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    public class MockContractRemarksManager : IContractRemarksManager
    {
        public static bool NonParallelizable = false;

        public async Task<CodeRemark> GetCodeRemarkAsync(IChainContext chainContext, Address address, Hash codeHash)
        {
            return await Task.FromResult(new CodeRemark
            {
                CodeHash = codeHash,
                NonParallelizable = NonParallelizable
            });
        }

        public void AddCodeHashCache(IBlockIndex blockIndex, Address address, Hash codeHash)
        {
            throw new System.NotImplementedException();
        }

        public async Task SetCodeRemarkAsync(Address address, Hash codeHash, BlockHeader blockHeader)
        {
            throw new System.NotImplementedException();
        }

        public void RemoveContractRemarksCache(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }

        public async Task SetIrreversedCacheAsync(List<BlockIndex> blockIndexes)
        {
            throw new System.NotImplementedException();
        }

        public bool MayHasContractRemarks(BlockIndex previousBlockIndex)
        {
            throw new System.NotImplementedException();
        }

        public Hash GetCodeHashByBlockIndex(BlockIndex previousBlockIndex, Address address)
        {
            throw new System.NotImplementedException();
        }
    }
}