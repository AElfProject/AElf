using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Windows.Input;
using AElf.Common.Extensions;
using AElf.Cryptography.ECDSA;
using AElf.Management.Commands;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using k8s;

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

        public void DeployMainChain(string chainId, DeployArg arg)
        {
            if (string.IsNullOrWhiteSpace(chainId))
            {
                chainId = SHA256.Create().ComputeHash(Guid.NewGuid().ToByteArray()).Take(ECKeyPair.AddressLength).ToArray().ToHex();
            }

            var commands = new List<IDeployCommand>();
            commands.Add(new K8SAddNamespaceCommand());
            commands.Add(new K8SAddRedisCommand());
            commands.Add(new K8SAddConfigCommand());
            commands.Add(new K8SAddAccountKeyCommand());
            commands.Add(new K8SAddManagerCommand());
            commands.Add(new K8SAddWorkerCommand());
            commands.Add(new K8SAddLauncherCommand());
            
            commands.ForEach(c => c.Action(chainId, arg));
        }

        public void RemoveMainChain(string chainId)
        {
            var commands = new List<IDeployCommand>();
            commands.Add(new K8SDeleteNamespaceCommand());

            commands.ForEach(c => c.Action(chainId, null));
        }

        public void DeploySideChain()
        {
            
        }

        public void RemoveSideChain()
        {
            
        }

        public void UpgreadeChain()
        {
            
        }
    }
}