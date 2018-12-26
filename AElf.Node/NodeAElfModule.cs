using System;
using System.IO;
using System.Net;
using System.Security;
using AElf.Common.Application;
using AElf.Common.Enums;
using AElf.Common.MultiIndexDictionary;
using AElf.Configuration;
using AElf.Configuration.Config.Consensus;
using AElf.Configuration.Config.Network;
using AElf.Configuration.Config.RPC;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Consensus;
using AElf.Kernel.Node;
using AElf.Modularity;
using AElf.Network;
using AElf.Node.AElfChain;
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
            ECKeyPair nodeKey = null;
            if (!string.IsNullOrWhiteSpace(NodeConfig.Instance.NodeAccount))
            {
                try
                {
                    var ks = new AElfKeyStore(ApplicationHelpers.ConfigPath);
                    
                    var pass = string.IsNullOrWhiteSpace(NodeConfig.Instance.NodeAccountPassword)
                        ? AskInvisible(NodeConfig.Instance.NodeAccount)
                        : NodeConfig.Instance.NodeAccountPassword;

                    ks.OpenAsync(NodeConfig.Instance.NodeAccount, pass, false).Wait();

                    NodeConfig.Instance.NodeAccountPassword = pass;

                    nodeKey = ks.GetAccountKeyPair(NodeConfig.Instance.NodeAccount);

                    if (nodeKey == null)
                    {
                        Console.WriteLine("Load keystore failed");
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Load keystore failed", e);
                }
            }

            TransactionPoolConfig.Instance.EcKeyPair = nodeKey;
            NetworkConfig.Instance.EcKeyPair = nodeKey;


            switch (ConsensusConfig.Instance.ConsensusType)
            {
                case ConsensusType.AElfDPoS:
                    context.Services.AddSingleton<IConsensus, DPoS>();
                    context.Services.AddTransient<ConsensusHelper>();
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

            if (NodeConfig.Instance.IsMiner && string.IsNullOrWhiteSpace(NodeConfig.Instance.NodeAccount))
            {
                throw new Exception("NodeAccount is needed");
            }

            NodeConfiguration confContext = new NodeConfiguration();
            confContext.KeyPair = TransactionPoolConfig.Instance.EcKeyPair;
            confContext.WithRpc = RpcConfig.Instance.UseRpc;
            confContext.LauncherAssemblyLocation = Path.GetDirectoryName(typeof(Node).Assembly.Location);

            var mainChainNodeService = context.ServiceProvider.GetRequiredService<INodeService>();
            var node = context.ServiceProvider.GetRequiredService<INode>();
            node.Register(mainChainNodeService);
            node.Initialize(confContext);
            node.Start();
        }
        
        private static string AskInvisible(string prefix)
        {
            Console.Write("Node account password: ");
            var pwd = new SecureString();
            while (true)
            {
                var i = Console.ReadKey(true);
                if (i.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (i.Key == ConsoleKey.Backspace)
                {
                    if (pwd.Length > 0)
                    {
                        pwd.RemoveAt(pwd.Length - 1);
                    }
                }
                else
                {
                    pwd.AppendChar(i.KeyChar);
                }
            }

            Console.WriteLine();
            return new NetworkCredential("", pwd).Password;
        }
    }
}