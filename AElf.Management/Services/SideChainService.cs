using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Management.Commands;
using AElf.Management.Handlers;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Common;
using Microsoft.Extensions.Options;

namespace AElf.Management.Services
{
    public class SideChainService : ISideChainService
    {
        private readonly ManagementOptions _managementOptions;
        
        public SideChainService(IOptionsSnapshot<ManagementOptions> options)
        {
            _managementOptions = options.Value;
        }

        public async Task Deploy(DeployArg arg)
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
                new K8SAddChainInfoCommand(),
                new K8SGrpcKeyCommand(),
                new K8SAddLighthouseCommand(),
                new K8SAddWorkerCommand(),
                new K8SAddLauncherCommand(),
                new K8SAddMonitorCommand()
            };

            foreach (var command in commands)
            {
                await command.Action(arg);
            }
        }

        public async Task Remove(string chainId)
        {
            await GetHandler().Execute(DeployType.Remove, chainId);
        }

        private IDeployHandler GetHandler()
        {
            return DeployHandlerFactory.GetHandler(_managementOptions.DeployType);
        }

        private string GenerateChainId()
        {
            throw new NotImplementedException();
        }
    }
}