using System.Threading.Tasks;
using AElf.Types;
using Volo.Abp.Threading;

namespace AElf.Kernel.SmartContract
{
    public class NullStateCache : IStateCache
    {
        public bool TryGetValue(ScopedStatePath key, out byte[] value)
        {
            value = null;
            return false;
        }

        public byte[] this[ScopedStatePath key]
        {
            get => null;
            set { }
        }
    }

    internal class ScopedStateProvider : IScopedStateProvider
    {
        public IHostSmartContractBridgeContext HostSmartContractBridgeContext { get; set; }
        public IStateCache Cache { get; set; } = new NullStateCache();

        public Address ContractAddress { get; set; }

        public byte[] Get(StatePath path)
        {
            var scoped = new ScopedStatePath()
            {
                Address = ContractAddress,
                Path = path
            };
            var byteString =
                AsyncHelper.RunSync(() => HostSmartContractBridgeContext.GetStateAsync(scoped.ToStateKey()));
            return byteString?.ToByteArray();
        }
    }
}