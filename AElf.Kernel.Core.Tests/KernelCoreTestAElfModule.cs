using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Database;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
using AElf.Modularity;
using AElf.TestBase;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.EventBus;
using Volo.Abp.Modularity;

namespace AElf.Kernel
{
    [DependsOn(
        typeof(AbpEventBusModule),
        typeof(CoreKernelAElfModule),
        typeof(TestBaseKernelAElfModule))]
    public class KernelCoreTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }

        public override void OnPreApplicationInitialization(ApplicationInitializationContext context)
        {

        }
    }
}