using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using AElf.Common.Application;
using AElf.Common.Enums;
using AElf.Common.MultiIndexDictionary;
using AElf.Configuration;
using AElf.Configuration.Config.Consensus;
using AElf.Configuration.Config.RPC;
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
            ECKeyPair nodeKey = null;
            if (!string.IsNullOrWhiteSpace(NodeConfig.Instance.NodeAccount))
            {
                try
                {
                    var keyStore = new AElfKeyStore(ApplicationHelpers.ConfigPath);
                    var password = string.IsNullOrWhiteSpace(NodeConfig.Instance.NodeAccountPassword) ? AskInvisible() : NodeConfig.Instance.NodeAccountPassword;
                    keyStore.OpenAsync(NodeConfig.Instance.NodeAccount, password, false).Wait();
                    NodeConfig.Instance.NodeAccountPassword = password;

                    nodeKey = keyStore.GetAccountKeyPair(NodeConfig.Instance.NodeAccount);
                    if (nodeKey == null)
                    {
                        Console.WriteLine("Load keystore failed.");
                        Environment.Exit(-1);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Load keystore failed.", e);
                }
            }

            TransactionPoolConfig.Instance.EcKeyPair = nodeKey;

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

        private static string AskInvisible()
        {
            Console.Write("Node account password: ");
            var securePassword = new SecureString();
            while (true)
            {
                var consoleKeyInfo = Console.ReadKey(true);
                if (consoleKeyInfo.Key == ConsoleKey.Enter)
                {
                    break;
                }

                if (consoleKeyInfo.Key == ConsoleKey.Backspace)
                {
                    if (securePassword.Length > 0)
                    {
                        securePassword.RemoveAt(securePassword.Length - 1);
                    }
                }
                else
                {
                    securePassword.AppendChar(consoleKeyInfo.KeyChar);
                }
            }

            Console.WriteLine();

            var intPtr = IntPtr.Zero;
            try
            {
                intPtr = Marshal.SecureStringToGlobalAllocUnicode(securePassword);
                return Marshal.PtrToStringUni(intPtr);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception while get account password.", ex);
            }
            finally
            {
                if (intPtr != IntPtr.Zero)
                    Marshal.ZeroFreeGlobalAllocUnicode(intPtr);
            }
        }
    }
}