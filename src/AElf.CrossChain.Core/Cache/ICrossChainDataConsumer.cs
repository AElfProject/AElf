
namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataConsumer
    {
        T TryTake<T>(int crossChainId, long height, bool isCacheSizeLimited);
    }
}