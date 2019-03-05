using System;
using AElf.Modularity;
using Volo.Abp.Modularity;

namespace AElf.Kernel.Tests
{
    [DependsOn(
        typeof(KernelAElfModule),
        typeof(KernelCoreTestAElfModule))]
    public class KernelTestAElfModule: AElfModule
    {
    }
}