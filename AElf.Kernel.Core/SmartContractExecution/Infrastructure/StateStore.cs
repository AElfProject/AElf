using System;
using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Infrastructure
{
    public class StateStore<T> : KeyValueStoreBase<StateKeyValueDbContext, T>, IStateStore<T>
    {
        public StateStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext)
            : base(byteSerializer, keyValueDbContext, StorePrefix.StatePrefix + typeof(T).Name)
        {
        }
    }

    
    //TODO: remove
    public class StateStore : KeyValueStoreBase<StateKeyValueDbContext>, IStateStore, ISingletonDependency
    {
        public StateStore(StateKeyValueDbContext keyValueDbContext)
            : base(new StateByteSerializer(), keyValueDbContext, StorePrefix.StatePrefix)
        {
        }

        public class StateByteSerializer : IByteSerializer
        {
            public byte[] Serialize(object obj)
            {
                return (byte[]) obj;
            }

            public T Deserialize<T>(byte[] bytes)
            {
                return (T) Convert.ChangeType(bytes, typeof(T));
            }
        }
    }

    public class BlockchainStore<T> : KeyValueStoreBase<BlockchainKeyValueDbContext, T>, IBlockchainStore<T>
    {
        public BlockchainStore(BlockchainKeyValueDbContext keyValueDbContext, IByteSerializer byteSerializer)
            : base(byteSerializer, keyValueDbContext, StorePrefix.StatePrefix  + typeof(T).Name)
        {
            
        }
    }
}