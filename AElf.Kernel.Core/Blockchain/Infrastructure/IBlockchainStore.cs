using AElf.Kernel.Infrastructure;
using Google.Protobuf;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public interface IBlockchainStore<T> : IKeyValueStore<T>
        where T : IMessage<T>, new()
    {
    }

    public class BlockchainStore<T> : KeyValueStoreBase<BlockchainKeyValueDbContext, T>, IBlockchainStore<T>
        where T : IMessage<T>, new()
    {
        public BlockchainStore(BlockchainKeyValueDbContext keyValueDbContext, IStoreKeyPrefixProvider<T> prefixProvider) : base(keyValueDbContext, prefixProvider)
        {
        }
    }
}