using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Network;
using AElf.Node.AElfChain;
using NLog;

namespace AElf.Node
{
    [LoggerName(nameof(Node))]
    public class Node : INode
    {
        private readonly ILogger _logger;
        private readonly IRpcServer _rpcServer;
        private readonly INetworkManager _netManager;

        private readonly List<INodeService> _services = new List<INodeService>();

        private bool _startRpc;

        public Node(ILogger logger, IRpcServer rpcServer, INetworkManager netManager)
        {
            _logger = logger;
            _rpcServer = rpcServer;
            _netManager = netManager;
        }

        public void Register(INodeService s)
        {
            _services.Add(s);
        }

        public void Initialize(NodeConfiguation conf)
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