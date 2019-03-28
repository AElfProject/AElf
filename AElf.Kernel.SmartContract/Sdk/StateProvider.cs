using System.Threading.Tasks;
using AElf.Kernel.Infrastructure;

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