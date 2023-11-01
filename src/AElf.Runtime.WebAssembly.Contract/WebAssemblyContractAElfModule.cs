using AElf.Kernel.SmartContract;
using AElf.Modularity;
using AElf.Runtime.CSharp;
using Volo.Abp.Modularity;

namespace AElf.Runtime.WebAssembly.Contract;

[DependsOn(
    typeof(SmartContractAElfModule),
    typeof(CSharpRuntimeAElfModule)
)]
public class WebAssemblyContractAElfModule : AElfModule
{
    
}