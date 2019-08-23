using System;
using AElf.BenchBase;
using AElf.Kernel.Consensus.AEDPoS.Application;
using AElf.Kernel.Consensus.Application;
using AElf.OS;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Core.Benches
{
    [DependsOn(
        typeof(OSCoreWithChainTestAElfModule)
    )]
    public class KernelCoreBenchAElfModule: BenchBaseAElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}