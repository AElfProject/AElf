using System.IO;
using System.Linq;
using AElf.Modularity;
using AElf.OS.Rpc;
using Akka.Actor;
using Akka.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Monitor
{
    [DependsOn(typeof(RpcAElfModule))]
    public class AkkaModule: AElfModule
    {
        public static IConfigurationRoot Configuration;
        
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.SetConfiguration(Configuration);
        }
        
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<ExecutionOptions>(configuration.GetSection("Execution"));
        }
        
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            //TODO! move to extensions method to support app.UseAkka()

            var executionOptions = context.ServiceProvider.GetService<IOptions<ExecutionOptions>>().Value;

            var clusterConfig = ConfigurationFactory.ParseString(File.ReadAllText("akka-monitor.hocon"));
            var systemName = clusterConfig.GetConfig("manager").GetString("system-name");
            var ipAddress = executionOptions.HostName;
            var port = executionOptions.Port;

            var seeds = string.Join(",",
                executionOptions.Seeds.Select(s => $@"""akka.tcp://{systemName}@{s.HostName}:{s.Port}"""));
            var seedsString = $"akka.cluster.seed-nodes = [{seeds}]";
            
            var finalConfig = ConfigurationFactory.ParseString(seedsString)
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.hostname=" + ipAddress))
                .WithFallback(ConfigurationFactory.ParseString("akka.remote.dot-netty.tcp.port=" + port))
                .WithFallback(clusterConfig);
            var actorSystem = ActorSystem.Create(systemName, finalConfig);
            
            actorSystem.ActorOf(Props.Create(typeof(AkkaClusterListener)), "clusterListener");
        }
    }
}