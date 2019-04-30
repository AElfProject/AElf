using System;
using AElf.BenchBase;
using AElf.OS;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Core.Benches
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class KernelCoreBenchAElfModule: BenchBaseAElfModule
    {
    }
}