using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Management.Commands;
using AElf.Management.Models;

namespace AElf.Management.Handlers
{
    public class K8SDeployHandler : IDeployHandler
    {
        private static readonly IDeployHandler _instance = new K8SDeployHandler();

        public static IDeployHandler Instance
        {
            get { return _instance; }
        }

        private readonly List<IDeployCommand> _deployCommands = new List<IDeployCommand>();
        private readonly List<IDeployCommand> _removeCommands = new List<IDeployCommand>();

        private K8SDeployHandler()
        {
            _deployCommands.Add(new K8SAddNamespaceCommand());
            _deployCommands.Add(new K8SAddRedisCommand());
            _deployCommands.Add(new K8SAddConfigCommand());
            _deployCommands.Add(new K8SAddAccountKeyCommand());
            _deployCommands.Add(new K8SAddManagerCommand());
            _deployCommands.Add(new K8SAddWorkerCommand());
            _deployCommands.Add(new K8SAddLauncherCommand());

            _removeCommands.Add(new K8SDeleteNamespaceCommand());
        }

        public void Execute(DeployType type, string chainId, DeployArg arg = null)
        {
            switch (type)
            {
                case DeployType.Deploy:
                    foreach (var cmd in _deployCommands)
                    {
                        cmd.Action(chainId, arg);
                    }
                    break;
                case DeployType.Remove:
                    foreach (var cmd in _removeCommands)
                    {
                        cmd.Action(chainId, arg);
                    }
                    break;
                default:
                    throw new ArgumentException("deploy type is incorrect");
            }
        }
    }
}