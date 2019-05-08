using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataProducer
    {
        bool AddNewBlockInfo(CrossChainCacheData crossChainCacheInfo);
        ILogger<CrossChainDataProducer> Logger { get; set; }
    }
}