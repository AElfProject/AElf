using AElf.ChainController;
using AElf.Common.Module;
using AElf.Configuration;
using AElf.Execution.Scheduling;
using Autofac;

namespace AElf.Execution
{
    public class ExecutionAElfModule:IAElfModlule
    {
        public void Init(ContainerBuilder builder)
        {
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