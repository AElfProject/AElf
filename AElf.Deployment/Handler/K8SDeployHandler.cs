using System.Collections.Generic;
using AElf.Deployment.Command;
using AElf.Deployment.Models;

namespace AElf.Deployment.Handler
{
    public class K8SDeployHandler : IDeployHandler
    {
        private static readonly IDeployHandler _instance = new K8SDeployHandler();

        public static IDeployHandler Instance
        {
            get { return _instance; }
        }
        
        private readonly List<IDeployCommand> _commands =new List<IDeployCommand>();

        private K8SDeployHandler()
        {
            _commands.Add(new K8SAddNamespaceCommand());
            _commands.Add(new K8SAddRedisCommand());
        }

        public void Deploy(DeployArgument arg)
        {
            foreach (var cmd in _commands)
            {
                cmd.Action(arg);
            }
        }
    }
}