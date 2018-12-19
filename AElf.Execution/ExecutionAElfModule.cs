using AElf.ChainController;
using AElf.Configuration;
using AElf.Execution.Execution;
using AElf.Execution.Scheduling;
using AElf.Modularity;
using Autofac;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Execution
{
    public class ExecutionAElfModule: AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
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
            }
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
        }

        public void Init(ContainerBuilder builder)
        {
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
            }
        }

        public void Run(ILifetimeScope scope)
        {
            if (NodeConfig.Instance.ExecutorType == "akka")
            {
                var actorEnv = scope.Resolve<IActorEnvironment>();
                actorEnv.InitActorSystem();
            }
        }
    }
}