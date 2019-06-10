using Google.Protobuf;

namespace AElf.CrossChain.Cache
{
    public interface IBlockCacheEntity : IMessage
    {
        long Height { get; set; }
        int ChainId { get; set;}
    }
}