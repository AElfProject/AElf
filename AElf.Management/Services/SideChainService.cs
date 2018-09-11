using System;
using System.Collections.Generic;
using AElf.Management.Commands;
using AElf.Management.Handlers;
using AElf.Management.Interfaces;
using AElf.Management.Models;

namespace AElf.Management.Services
{
    public class SideChainService : ISideChainService
    {
        public void Deploy(string chainId, DeployArg arg)
        {
            if (string.IsNullOrWhiteSpace(arg.MainChainId))
            {
                throw new ArgumentException("main chain id is empty!");
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

        public void Remove(string chainId)
        {
            GetHandler().Execute(DeployType.Remove, chainId);
        }

        private IDeployHandler GetHandler()
        {
            return DeployHandlerFactory.GetHandler();
        }
    }
}