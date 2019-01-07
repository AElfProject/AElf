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
        public StateStore(IByteSerializer byteSerializer, StateKeyValueDbContext keyValueDbContext, string dataPrefix)
            : base(byteSerializer, keyValueDbContext, GlobalConfig.StatePrefix)
        {
        }
    }
}