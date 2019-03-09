using AElf.Kernel.SmartContract;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractBridge
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class SmartContractBridgeKernelAElfModule : AElfModule<SmartContractBridgeKernelAElfModule>
    {
    }
}