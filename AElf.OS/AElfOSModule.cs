using AElf.Kernel;
using AElf.Modularity;
using AElf.OS.Network;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AElf.OS
{
    [DependsOn(typeof(KernelAElfModule))]
    public class AElfOSModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
        }
    }
}