using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Common.Serializers;
using AElf.Database;
using AElf.Kernel.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Storages
{
    public class StateStore<T> : KeyValueStoreBase<StateKeyValueDbContext, T>, IStateStore<T>
    {
        public StateStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext)
            : base(byteSerializer, keyValueDbContext, GlobalConfig.StatePrefix + typeof(T).Name)
        {
        }
    }

    
    //TODO: remove
    public class StateStore : KeyValueStoreBase<StateKeyValueDbContext>, IStateStore, ISingletonDependency
    {
        public StateStore(StateKeyValueDbContext keyValueDbContext)
            : base(new StateByteSerializer(), keyValueDbContext, GlobalConfig.StatePrefix)
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
            : base(byteSerializer, keyValueDbContext, GlobalConfig.StatePrefix  + typeof(T).Name)
        {
            
        }
    }
}