
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.SmartContractExecution.Scheduling;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Kernel.SmartContractExecution
{
    [DependsOn(typeof(CoreKernelAElfModule))]
    public class SmartContractExecutionAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            //var configuration = context.Services.GetConfiguration();
            //Configure<ExecutionOptions>(configuration.GetSection("Execution"));
            
            //var assembly = typeof(ParallelTransactionExecutingService).Assembly;

            var services = context.Services;

            services.AddAssemblyOf<SmartContractExecutionAElfModule>();
            
            
            services.AddTransient<IGrouper,Grouper>();
            services.AddTransient<IResourceUsageDetectionService,ResourceUsageDetectionService>();
            
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            //var executorType = context.ServiceProvider.GetService<IOptionsSnapshot<ExecutionOptions>>().Value.ExecutorType;
        }

    }
}