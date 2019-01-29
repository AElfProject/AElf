using AElf.ChainController;
using AElf.Configuration;
using AElf.Execution.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Execution
{
    [DependsOn(typeof(KernelAElfModule))]
    public class ExecutionAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<ExecutionOptions>(configuration.GetSection("Execution"));
            
            var assembly = typeof(ParallelTransactionExecutingService).Assembly;

            var services = context.Services;

            services.AddAssemblyOf<ExecutionAElfModule>();
            
            
            //TODO! move into a new project, remove if statement
            services.AddTransient<ServicePack>();
            services.AddTransient<IActorEnvironment,ActorEnvironment>();
            services.AddTransient<IGrouper,Grouper>();
            services.AddTransient<IResourceUsageDetectionService,ResourceUsageDetectionService>();
            services.AddTransient<ParallelTransactionExecutingService>();
            services.AddTransient<NoFeeSimpleExecutingService>();
            services.AddTransient<SimpleExecutingService>();
            services.AddTransient<IExecutingService>(provider =>
            {
                var executorType = provider.GetService<IOptionsSnapshot<ExecutionOptions>>().Value.ExecutorType;
                if (executorType == "akka")
                {
                    return provider.GetService<ParallelTransactionExecutingService>();
                }

                if (executorType == "nofee")
                {
                    return provider.GetService<NoFeeSimpleExecutingService>();
                }

                return provider.GetService<SimpleExecutingService>();
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            //TODO! remove if statement from config
            var executorType = context.ServiceProvider.GetService<IOptionsSnapshot<ExecutionOptions>>().Value.ExecutorType;
            if (executorType == "akka")
            {
                //TODO! change to userAkka():

                var actorEnv = context.ServiceProvider.GetService<IActorEnvironment>();
                actorEnv.InitActorSystem();
            }
        }

    }
}