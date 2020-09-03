using AElf.Runtime.CSharp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContract.Parallel.Tests
{
    [DependsOn(typeof(ParallelExecutionModule),
        typeof(KernelCoreTestAElfModule),
        typeof(CSharpRuntimeAElfModule))]
    public class SmartContractParallelTestAElfModule
    {
        
    }
}