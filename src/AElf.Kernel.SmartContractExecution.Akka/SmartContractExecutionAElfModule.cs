using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution.Akka
{
    [DependsOn(typeof(SmartContractExecutionAElfModule))]
    public class AkkaSmartContractExecutionAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;

            services.AddAssemblyOf<AkkaSmartContractExecutionAElfModule>();
            
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            
        }

    }
}