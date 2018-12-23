using AElf.ChainController;
using AElf.Configuration;
using AElf.Execution.Execution;
using AElf.Execution.Scheduling;
using AElf.Kernel;
using AElf.Modularity;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Execution
{
    [DependsOn(typeof(KernelAElfModule))]
    public class ExecutionAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var assembly = typeof(ParallelTransactionExecutingService).Assembly;

            var services = context.Services;

            services.AddAssemblyOf<ExecutionAElfModule>();
            
            
            //TODO! move into a new project, remove if statement
            if (NodeConfig.Instance.ExecutorType == "akka")
            {
                
                
                services.AddTransient<IExecutingService,ParallelTransactionExecutingService>();
                services.AddTransient<ServicePack>();
                services.AddTransient<IActorEnvironment,ActorEnvironment>();
                services.AddTransient<IGrouper,Grouper>();
                services.AddTransient<IResourceUsageDetectionService,ResourceUsageDetectionService>();

            }
            else
            {
                // services were auto registered.
                services.AddTransient<IExecutingService,SimpleExecutingService>();
            }
            
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            //TODO! remove if statement from config
            if (NodeConfig.Instance.ExecutorType == "akka")
            {
                //TODO! change to userAkka():

                var actorEnv = context.ServiceProvider.GetService<IActorEnvironment>();
                actorEnv.InitActorSystem();
            }
        }

    }
}