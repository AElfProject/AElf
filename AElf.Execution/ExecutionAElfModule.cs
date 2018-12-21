using AElf.ChainController;
using AElf.Configuration;
using AElf.Execution.Execution;
using AElf.Execution.Scheduling;
using AElf.Modularity;
using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Execution
{
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
                
                //TODO! not implement akka
                /*
                builder.RegisterType<ResourceUsageDetectionService>().As<IResourceUsageDetectionService>();
                builder.RegisterType<Grouper>().As<IGrouper>();
                builder.RegisterType<ServicePack>().PropertiesAutowired();
                builder.RegisterType<ActorEnvironment>().As<IActorEnvironment>().SingleInstance();
                builder.RegisterType<ParallelTransactionExecutingService>().As<IExecutingService>();*/
                
            }
            else
            {
                // services were auto registered.
                
                //builder.RegisterType<SimpleExecutingService>().As<IExecutingService>();
            }
            
            /*
            var assembly = typeof(ParallelTransactionExecutingService).Assembly;

            
            builder.RegisterAssemblyTypes(assembly).AsImplementedInterfaces();
            
            if (NodeConfig.Instance.ExecutorType == "akka")
            {
                builder.RegisterType<ResourceUsageDetectionService>().As<IResourceUsageDetectionService>();
                builder.RegisterType<Grouper>().As<IGrouper>();
                builder.RegisterType<ServicePack>().PropertiesAutowired();
                builder.RegisterType<ActorEnvironment>().As<IActorEnvironment>().SingleInstance();
                builder.RegisterType<ParallelTransactionExecutingService>().As<IExecutingService>();
            }
            else
            {
                builder.RegisterType<SimpleExecutingService>().As<IExecutingService>();
            }*/
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            if (NodeConfig.Instance.ExecutorType == "akka")
            {
                //TODO! change to userAkka():

                //var actorEnv = scope.Resolve<IActorEnvironment>();
                //actorEnv.InitActorSystem();
            }
        }

    }
}