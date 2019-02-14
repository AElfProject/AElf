using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController.EventMessages;
using AElf.Common;
using AElf.Kernel;
using AElf.Network;
using AElf.Node.AElfChain;
using AElf.Node.EventMessages;
using Easy.MessageHub;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AElf.Node
{
    public class Node : INode, ITransientDependency
    {
        public ILogger<Node> Logger {get;set;}
        private readonly INetworkManager _netManager;
        private readonly ChainOptions _chainOptions;

        private readonly List<INodeService> _services = new List<INodeService>();

        private bool _startRpc;

        public Node( INetworkManager netManager, IOptionsSnapshot<ChainOptions> options)
        {
            Logger = NullLogger<Node>.Instance;
            _netManager = netManager;
            _chainOptions = options.Value;
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
                service.Initialize(_chainOptions.ChainId.ConvertBase58ToChainId(), conf);
            }
        }

        public bool Start()
        {
            if (_startRpc)
                StartRpc();

            Task.Run(() => _netManager.Start());

            foreach (var service in _services)
            {
                service.Start(_chainOptions.ChainId.ConvertBase58ToChainId());
            }

            return true;
        }

        public bool StartRpc()
        {
            return true;
        }
    }
}