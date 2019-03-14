using System.Collections.Generic;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataProducer
    {
        bool AddNewBlockInfo(IBlockInfo blockInfo);
        long GetChainHeightNeeded(int chainId);
        IEnumerable<int> GetCachedChainIds();
    }
}