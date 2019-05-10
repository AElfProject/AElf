using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataProducer
    {
        bool AddCacheEntity(BlockCacheEntity blockCacheEntity);
        ILogger<CrossChainDataProducer> Logger { get; set; }
    }
}