using Google.Protobuf;

namespace AElf.CrossChain.Cache
{
    public class BlockCacheEntity
    {
        public long Height { get; set; }
        public int ChainId { get; set;}
        public ByteString Payload { get; set;}
    }
}