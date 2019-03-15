using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataProducer
    {
        bool AddNewBlockInfo(IBlockInfo blockInfo);
        long GetChainHeightNeeded(int chainId);
        IEnumerable<int> GetCachedChainIds();
        ILogger<CrossChainDataProducer> Logger { get; set; }
    }
}