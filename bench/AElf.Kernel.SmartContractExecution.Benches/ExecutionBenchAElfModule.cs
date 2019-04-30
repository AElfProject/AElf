using AElf.BenchBase;
using AElf.OS;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution.Benches
{
    [DependsOn(
        typeof(BenchBaseAElfModule),
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class ExecutionBenchAElfModule: BenchBaseAElfModule
    {
        
    }
}