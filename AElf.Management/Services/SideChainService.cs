using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using AElf.Common.Extensions;
using AElf.Cryptography.ECDSA;
using AElf.Management.Commands;
using AElf.Management.Handlers;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Common;

namespace AElf.Management.Services
{
    public class SideChainService : ISideChainService
    {
        public void Deploy(DeployArg arg)
        {
            if (string.IsNullOrWhiteSpace(arg.MainChainId))
            {
                throw new ArgumentException("main chain id is empty!");
            }
            if (string.IsNullOrWhiteSpace(arg.SideChainId))
            {
                arg.SideChainId = GenerateChainId();
            }
            arg.IsDeployMainChain = false;

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

        public void Remove(string chainId)
        {
            GetHandler().Execute(DeployType.Remove, chainId);
        }

        private IDeployHandler GetHandler()
        {
            return DeployHandlerFactory.GetHandler();
        }
        
        private string GenerateChainId()
        {
            return SHA256.Create().ComputeHash(Guid.NewGuid().ToByteArray()).Take(Globals.AddressLength).ToArray().ToHex();
        }
    }
}