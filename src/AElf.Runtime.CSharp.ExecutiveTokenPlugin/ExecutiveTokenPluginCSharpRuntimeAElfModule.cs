using AElf.Kernel.SmartContract;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Runtime.CSharp.ExecutiveTokenPlugin
{
    [DependsOn(typeof(SmartContractAElfModule))]
    public class ExecutiveTokenPluginCSharpRuntimeAElfModule : AElfModule
    {
    }
}