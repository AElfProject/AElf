using System.IO;
using AElf.Kernel;
using AElf.Modularity;
using AElf.Network;
using AElf.Node.AElfChain;
using AElf.Synchronization;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Node
{
    [DependsOn(typeof(NetworkAElfModule),
        typeof(SyncAElfModule),
        typeof(KernelAElfModule))]
    public class NodeAElfModule : AElfModule
    {
        
        //TODO! change implements

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            NodeConfiguration confContext = new NodeConfiguration();
            confContext.LauncherAssemblyLocation = Path.GetDirectoryName(typeof(Node).Assembly.Location);

            var mainChainNodeService = context.ServiceProvider.GetRequiredService<INodeService>();
            var node = context.ServiceProvider.GetRequiredService<INode>();
            node.Register(mainChainNodeService);
            node.Initialize(confContext);
            node.Start();
        }
    }
}