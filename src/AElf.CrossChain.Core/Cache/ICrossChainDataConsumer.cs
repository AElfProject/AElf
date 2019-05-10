using Google.Protobuf;

namespace AElf.CrossChain.Cache
{
    public interface ICrossChainDataConsumer
    {
        T Take<T>(int crossChainId, long height, bool isCacheSizeLimited) where T : IMessage, new();
    }
}