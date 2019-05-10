using Microsoft.Extensions.Logging;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataProducer
    {
        bool TryAddBlockCacheEntity(BlockCacheEntity blockCacheEntity);
        ILogger<CrossChainDataProducer> Logger { get; set; }
    }
}