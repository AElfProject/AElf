using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.OS.Network.Infrastructure;
using AElf.OS.Node.Application;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkServer : IAElfNetworkServer, ISingletonDependency
    {
        private readonly NetworksOptions _networksOptions;

        private Server _server;

        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }

        private readonly IBlockchainService _blockchainService;
        private readonly IAccountService _accountService;

        public GrpcNetworkServer(IBlockchainService blockchainService, IOptionsSnapshot< NetworksOptions> networksOptions, 
            IAccountService accountService)
        {
            _blockchainService = blockchainService;
            _networksOptions = networksOptions.Value;
            _accountService = accountService;

            Logger = NullLogger<GrpcNetworkServer>.Instance;
            EventBus = NullLocalEventBus.Instance;
        }

        public async Task<IDisposable> StartAsync(int chainId)
        {
            var options = _networksOptions.GetOrDefault(chainId);
            PeerPool=new GrpcPeerPool(chainId, options,_accountService,_blockchainService );
            _server = new Server
            {
                Services = {PeerService.BindService(new GrpcServerService(chainId, PeerPool, _blockchainService))},
                Ports =
                {
                    new ServerPort(IPAddress.Any.ToString(), options.ListeningPort, ServerCredentials.Insecure)
                }
            };

            await Task.Run(() => _server.Start());

            // Add the provided boot nodes
            if (options.BootNodes != null && options.BootNodes.Any())
            {
                List<Task<bool>> taskList = options.BootNodes.Select(PeerPool.AddPeerAsync).ToList();
                await Task.WhenAll(taskList.ToArray<Task>());
            }
            else
            {
                Logger.LogWarning("Boot nodes list is empty.");
            }

            return this;
        }

        public async Task StopAsync()
        {
            await _server.KillAsync();

            foreach (var peer in PeerPool.GetPeers())
            {
                try
                {
                    await peer.SendDisconnectAsync();
                    await peer.StopAsync();
                }
                catch (Exception e)
                {
                    Logger.LogError(e, $"Error while disconnecting peer {peer}.");
                }
            }
        }

        public IPeerPool PeerPool { get; private set; }

        public void Dispose()
        {
        }
    }
}