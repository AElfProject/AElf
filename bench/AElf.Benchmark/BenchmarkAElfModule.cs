using AElf.Modularity;
using AElf.OS;
using Volo.Abp.Modularity;

namespace AElf.Benchmark
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class BenchmarkAElfModule: AElfModule
    {
        
    }
}