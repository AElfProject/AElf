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

        public void DeployMainChain(DeployArg arg)
        {
            if (string.IsNullOrWhiteSpace(arg.MainChainId))
            {
                arg.MainChainId = GenerateChainId();
                arg.IsDeployMainChain = true;
                arg.SideChainId = arg.MainChainId;
            }

            var commands = new List<IDeployCommand>
            {
                new K8SAddNamespaceCommand(), 
                new K8SAddRedisCommand(),
                new K8SAddLauncherServiceCommand(),
                new K8SAddConfigCommand(), 
                new K8SAddAccountKeyCommand(), 
                new K8SGrpcKeyCommand(),
                new K8SAddLighthouseCommand(), 
                new K8SAddWorkerCommand(), 
                new K8SAddLauncherCommand(),
                new K8SAddMonitorCommand()
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

        public DeployTestChainResult DeployTestChain()
        {
            var chainId = GenerateChainId();
            var password = "123";
            var accounts = CreateAccount(1, password);
            
            var arg1 = new DeployArg();
            arg1.ChainAccount = accounts[0];
            arg1.AccountPassword = password;
            arg1.DBArg = new DeployDBArg();
            arg1.LighthouseArg=new DeployLighthouseArg();
            arg1.LighthouseArg.IsCluster = false;
            arg1.WorkArg = new DeployWorkArg();
            arg1.WorkArg.WorkerCount = 2;
            arg1.WorkArg.ActorCount = 4;
            arg1.LauncherArg=new DeployLauncherArg();
            arg1.LauncherArg.IsConsensusInfoGenerator = true;
            arg1.Miners = accounts;

            var namespace1 = chainId + "-1";

            DeployMainChain(arg1);
            return null;
            
            string host1 = null;
            while (true)
            {
                Thread.Sleep(3000);
                var service1 = K8SRequestHelper.GetClient().ReadNamespacedService(GlobalSetting.LauncherServiceName, namespace1);
                var ingress = service1.Status.LoadBalancer.Ingress;
                if (ingress == null)
                {
                    continue;
                }
                host1 = ingress.FirstOrDefault().Hostname;
                if (string.IsNullOrWhiteSpace(host1))
                {
                    continue;
                }

                var pod1 = K8SRequestHelper.GetClient().ListNamespacedPod(namespace1, labelSelector: "name=" + GlobalSetting.LauncherName);
                if (pod1 == null || pod1.Items.First().Status.Phase != "Running")
                {
                    continue;
                }

                Thread.Sleep(60000);
                break;
            }

            var arg2 = new DeployArg();
            arg2.ChainAccount = accounts[1];
            arg2.AccountPassword = password;
            arg2.DBArg = new DeployDBArg();
            arg2.LighthouseArg=new DeployLighthouseArg();
            arg2.LighthouseArg.IsCluster = true;
            arg2.WorkArg = new DeployWorkArg();
            arg2.WorkArg.WorkerCount = 2;
            arg2.WorkArg.ActorCount = 4;
            arg2.LauncherArg=new DeployLauncherArg();
            arg2.LauncherArg.IsConsensusInfoGenerator = false;
            arg2.LauncherArg.Bootnodes = new List<string> {host1 + ":30800"};
            arg2.Miners = accounts;
            
            var namespace2 = chainId + "-2";

            DeployMainChain(arg2);
            
            string host2 = null;
            while (true)
            {
                Thread.Sleep(3000);
                var service2 = K8SRequestHelper.GetClient().ReadNamespacedService(GlobalSetting.LauncherServiceName, namespace2);
                var ingress = service2.Status.LoadBalancer.Ingress;
                if (ingress == null)
                {
                    continue;
                }
                host2 = ingress.FirstOrDefault().Hostname;
                if (string.IsNullOrWhiteSpace(host2))
                {
                    continue;
                }

                var pod2 = K8SRequestHelper.GetClient().ListNamespacedPod(namespace2, labelSelector: "name=" + GlobalSetting.LauncherName);
                if (pod2 == null || pod2.Items.First().Status.Phase != "Running")
                {
                    continue;
                }

                Thread.Sleep(60000);
                break;
            }
            
            var arg3 = new DeployArg();
            arg3.ChainAccount = accounts[2];
            arg3.AccountPassword = password;
            arg3.DBArg = new DeployDBArg();
            arg3.LighthouseArg=new DeployLighthouseArg();
            arg3.LighthouseArg.IsCluster = true;
            arg3.WorkArg = new DeployWorkArg();
            arg3.WorkArg.WorkerCount = 2;
            arg3.WorkArg.ActorCount = 4;
            arg3.LauncherArg=new DeployLauncherArg();
            arg3.LauncherArg.IsConsensusInfoGenerator = false;
            arg3.LauncherArg.Bootnodes=new List<string>
            {
                host1 + ":30800",
                host2 + ":30800"
            };
            arg3.Miners = accounts;
            
            var namespace3 = chainId + "-3";

            DeployMainChain(arg3);
            
            string host3 = null;
            while (true)
            {
                Thread.Sleep(3000);
                var service3 = K8SRequestHelper.GetClient().ReadNamespacedService(GlobalSetting.LauncherServiceName, namespace3);
                var ingress = service3.Status.LoadBalancer.Ingress;
                if (ingress == null)
                {
                    continue;
                }
                host3 = ingress.FirstOrDefault().Hostname;
                if (string.IsNullOrWhiteSpace(host3))
                {
                    continue;
                }
                break;
            }

            return new DeployTestChainResult
            {
                ChainId = chainId,
                NodePort = 30060,
                RpcPort = 30080,
                NodeHost = new List<string>
                {
                    host1,
                    host2,
                    host3
                }
            };
        }

        public void RemoveTestChain(string chainId)
        {
            for (var i = 1; i <= 3; i++)
            {
                var namespaced = chainId + "-" + i;
                RemoveMainChain(namespaced);
            }
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