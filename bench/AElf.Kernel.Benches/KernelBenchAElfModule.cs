using System;
using AElf.BenchBase;
using AElf.OS;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Benches
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class KernelBenchAElfModule: BenchBaseAElfModule
    {
        
    }
}