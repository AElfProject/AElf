using AElf.Common.Serializers;
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
        public BlockchainStore(BlockchainKeyValueDbContext keyValueDbContext)
            : base(keyValueDbContext)
        {
        }

        protected override string GetDataPrefix()
        {
            return typeof(T).Name;
        }
    }
}