using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using AElf.Common.Application;
using AElf.Common.Enums;
using AElf.Common.MultiIndexDictionary;
using AElf.Configuration;
using AElf.Configuration.Config.Consensus;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Consensus;
using AElf.Kernel.Node;
using AElf.Modularity;
using AElf.Network;
using AElf.Node.AElfChain;
using AElf.Node.Consensus;
using AElf.Node.Protocol;
using AElf.Synchronization.BlockSynchronization;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace AElf.Node
{
    [DependsOn(typeof(AElf.Network.NetworkAElfModule),
        typeof(AElf.Synchronization.SyncAElfModule),
        typeof(AElf.Kernel.KernelAElfModule))]
    public class NodeAElfModule : AElfModule
    {
        
        //TODO! change implements

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            switch (ConsensusConfig.Instance.ConsensusType)
            {
                case ConsensusType.AElfDPoS:
                    context.Services.AddSingleton<IConsensus, DPoS>();
                    context.Services.AddSingleton<ConsensusHelper>();
                    break;
                case ConsensusType.PoW:
                    break;
                case ConsensusType.SingleNode:
                    break;
            }
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            Console.WriteLine($"Using consensus: {ConsensusConfig.Instance.ConsensusType}");

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