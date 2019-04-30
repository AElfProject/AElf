using System;
using AElf.BenchBase;
using AElf.OS;
using Volo.Abp.Modularity;

namespace AElf.Kernel.TransactionPool.Benches
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class TransactionPoolBenchAElfModule: BenchBaseAElfModule
    {
    }
}