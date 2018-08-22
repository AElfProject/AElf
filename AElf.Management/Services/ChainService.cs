using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Input;
using AElf.Common.Application;
using AElf.Common.Extensions;
using AElf.Cryptography;
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
                chainId = GenerateChainId();
            }

            var commands = new List<IDeployCommand>
            {
                new K8SAddNamespaceCommand(), 
                new K8SAddRedisCommand(), 
                new K8SAddConfigCommand(), 
                new K8SAddAccountKeyCommand(), 
                new K8SAddManagerCommand(), 
                new K8SAddWorkerCommand(), 
                new K8SAddLauncherCommand()
            };

            commands.ForEach(c => c.Action(chainId, arg));
        }

        public void RemoveMainChain(string chainId)
        {
            var commands = new List<IDeployCommand>
            {
                new K8SDeleteNamespaceCommand()
            };

            commands.ForEach(c => c.Action(chainId, null));
        }

        public void DeployTestChain()
        {
            var chainId = GenerateChainId();
            var password = "123";
            var accounts = CreateAccount(3, password);
            
            var arg1 = new DeployArg();
            arg1.MainChainAccount = accounts[0];
            arg1.AccountPassword = password;
            arg1.DBArg = new DeployDBArg();
            arg1.ManagerArg=new DeployManagerArg();
            arg1.ManagerArg.IsCluster = false;
            arg1.WorkArg = new DeployWorkArg();
            arg1.WorkArg.ActorCount = 4;
            arg1.LauncherArg=new DeployLauncherArg();
            arg1.LauncherArg.IsConsensusInfoGenerator = true;

            DeployMainChain(chainId + "-1", arg1);
            
            Thread.Sleep(5000);
            var service1 = K8SRequestHelper.GetClient().ReadNamespacedService("service-launcher", chainId + "-1");
            var host1 = service1.Spec.ExternalIPs.First();
            
            var arg2 = new DeployArg();
            arg2.MainChainAccount = accounts[1];
            arg2.AccountPassword = password;
            arg2.DBArg = new DeployDBArg();
            arg2.ManagerArg=new DeployManagerArg();
            arg2.ManagerArg.IsCluster = false;
            arg2.WorkArg = new DeployWorkArg();
            arg2.WorkArg.ActorCount = 4;
            arg2.LauncherArg=new DeployLauncherArg();
            arg2.LauncherArg.IsConsensusInfoGenerator = false;
            arg2.LauncherArg.Bootnodes = new List<string> {host1 + ":30800"};

            DeployMainChain(chainId + "-2", arg2);
            
            Thread.Sleep(5000);
            var service2 = K8SRequestHelper.GetClient().ReadNamespacedService("service-launcher", chainId + "-2");
            var host2 = service2.Spec.ExternalIPs.First();
            
            var arg3 = new DeployArg();
            arg3.MainChainAccount = accounts[2];
            arg3.AccountPassword = password;
            arg3.DBArg = new DeployDBArg();
            arg3.ManagerArg=new DeployManagerArg();
            arg3.ManagerArg.IsCluster = false;
            arg3.WorkArg = new DeployWorkArg();
            arg3.WorkArg.ActorCount = 4;
            arg3.LauncherArg=new DeployLauncherArg();
            arg3.LauncherArg.IsConsensusInfoGenerator = false;
            arg3.LauncherArg.Bootnodes=new List<string>
            {
                host1 + ":30800",
                host2 + ":30800"
            };

            DeployMainChain(chainId + "-3", arg3);
        }

        private List<string> CreateAccount(int num,string password)
        {
            var result =new List<string>();
            for (var i = 0; i < num; i++)
            {
                var keyStore = new AElfKeyStore(ApplicationHelpers.GetDefaultDataDir());
                var key = keyStore.Create(password);
                var account = key.GetAddressHex();
                result.Add(account);
            }

            return result;
        }

        public void UpgreadeChain()
        {
            
        }

        private string GenerateChainId()
        {
            return SHA256.Create().ComputeHash(Guid.NewGuid().ToByteArray()).Take(ECKeyPair.AddressLength).ToArray().ToHex();
        }
    }
}