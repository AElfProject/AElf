using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.ExecutionPluginForAcs1
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutionPluginForAcs1Module : AElfModule
    {
    }
}