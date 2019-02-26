using System;
using AElf.Common;
using AElf.Common.Serializers;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContractExecution.Infrastructure
{
    public class StateStore<T> : KeyValueStoreBase<StateKeyValueDbContext, T>, IStateStore<T>
        where T : IMessage<T>, new()
    {
        public StateStore(StateKeyValueDbContext keyValueDbContext) : base(keyValueDbContext)
        {
        }

        protected override string GetDataPrefix()
        {
            return typeof(T).Name;
        }
    }
}