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
}