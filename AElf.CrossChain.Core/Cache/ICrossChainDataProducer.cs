using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataProducer
    {
        ILogger<CrossChainDataProducer> Logger { get; set; }
        bool AddNewBlockInfo(IBlockInfo blockInfo);
        long GetChainHeightNeeded(int chainId);
        IEnumerable<int> GetCachedChainIds();
    }
}