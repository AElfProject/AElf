using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common.Attributes;
using AElf.Network;
using AElf.Node.AElfChain;
using AElf.Node.EventMessages;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Node
{
    [LoggerName(nameof(Node))]
    public class Node : INode, ITransientDependency
    {
        public ILogger<Node> Logger {get;set;}
        private readonly IRpcServer _rpcServer;
        private readonly INetworkManager _netManager;

        private readonly List<INodeService> _services = new List<INodeService>();

        private bool _startRpc;

        public Node( IRpcServer rpcServer, INetworkManager netManager)
        {
            Logger = NullLogger<Node>.Instance;
            _rpcServer = rpcServer;
            _netManager = netManager;
        }

        public void Register(INodeService s)
        {
            _services.Add(s);
        }

        public void Initialize(NodeConfiguration conf)
        {
            _startRpc = conf.WithRpc;

            foreach (var service in _services)
            {
                service.Initialize(conf);
            }
        }

        public bool Start()
        {
            if (_startRpc)
                StartRpc();

            Task.Run(() => _netManager.Start());

            foreach (var service in _services)
            {
                service.Start();
            }

            return true;
        }

        public bool StartRpc()
        {
            _rpcServer.Start();
            return true;
        }
    }
}