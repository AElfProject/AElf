using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Infrastructure;

namespace AElf.Kernel.SmartContract.Sdk
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

        public async Task<byte[]> GetAsync(StatePath path)
        {
            var scoped = new ScopedStatePath()
            {
                Address = ContractAddress,
                Path = path
            };
            var byteString = await HostSmartContractBridgeContext.GetStateAsync(scoped.ToStateKey());
            return byteString?.ToByteArray();
        }
    }
}