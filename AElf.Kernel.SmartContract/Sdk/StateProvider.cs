using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;

namespace AElf.Kernel.SmartContract.Sdk
{
    public class NullStateCache : IStateCache
    {
        public bool TryGetValue(StatePath key, out byte[] value)
        {
            value = null;
            return false;
        }

        public byte[] this[StatePath key]
        {
            get => null;
            set { }
        }
    }

    internal class StateProvider : IStateProvider
    {
        public IHostSmartContractBridgeContext HostSmartContractBridgeContext { get; set; }
        public IStateCache Cache { get; set; } = new NullStateCache();

        public async Task<byte[]> GetAsync(StatePath path)
        {
            var byteString = await HostSmartContractBridgeContext.GetStateAsync(path.ToStorageKey());
            return byteString?.ToByteArray();
        }
    }
}