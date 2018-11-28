using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using AElf.Management.Commands;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using k8s;
using AElf.Common;

namespace AElf.Management.Services
{
    public class ChainService : IChainService
    {
        public List<ChainResult> GetAllChains()
        {
            var namespaces = K8SRequestHelper.GetClient().ListNamespace();

            return (from np in namespaces.Items
                where np.Metadata.Name != "default" && np.Metadata.Name != "kube-system" && np.Metadata.Name != "kube-public"
                select new ChainResult
                {
                    ChainId = np.Metadata.Name,
                    Status = np.Status.Phase,
                    CreateTime = np.Metadata.CreationTimestamp
                }).ToList();
        }

        public void DeployMainChain(DeployArg arg)
        {
            if (string.IsNullOrWhiteSpace(arg.MainChainId))
            {
                arg.MainChainId = GenerateChainId();
            }
            arg.IsDeployMainChain = true;
            arg.SideChainId = arg.MainChainId;

            var commands = new List<IDeployCommand>
            {
                new K8SAddNamespaceCommand(), 
                new K8SAddRedisCommand(),
                new K8SAddLauncherServiceCommand(),
                new K8SAddAccountKeyCommand(), 
                new K8SAddConfigCommand(), 
                new K8SAddChainInfoCommand(),
                new K8SGrpcKeyCommand(),
                new K8SAddLighthouseCommand(), 
                new K8SAddWorkerCommand(), 
                new K8SAddLauncherCommand(),
                new K8SAddMonitorCommand(),
                new SaveApiKeyCommand(),
                new AddMonitorDBCommand()
            };

            commands.ForEach(c => c.Action(arg));
        }

        public void RemoveMainChain(string chainId)
        {
            var commands = new List<IDeployCommand>
            {
                new K8SDeleteNamespaceCommand()
            };

            commands.ForEach(c => c.Action(new DeployArg {SideChainId = chainId}));
        }

        private string GenerateChainId()
        {
            return SHA256.Create().ComputeHash(Guid.NewGuid().ToByteArray()).Take(GlobalConfig.ChainIdLength).ToArray().ToHex();
        }
    }
}