using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Account;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Grpc.Events;
using AElf.OS.Network.Temp;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Utilities.Encoders;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkServer : IAElfNetworkServer, IPeerAuthentificator, ISingletonDependency
    {
        private readonly NetworkOptions _networkOptions;
        
        private readonly IAccountService _accountService;
        private readonly IBlockService _blockService;
        
        private Server _server;
        private readonly List<GrpcPeer> _authenticatedPeers;
        
        public ILocalEventBus EventBus { get; set; }
        public ILogger<GrpcNetworkServer> Logger { get; set; }
        
        public GrpcNetworkServer(IOptionsSnapshot<NetworkOptions> options, IAccountService accountService, 
            IBlockService blockService)
        {
            _accountService = accountService;
            _blockService = blockService;
            _networkOptions = options.Value;
            
            Logger = NullLogger<GrpcNetworkServer>.Instance;
            EventBus = NullLocalEventBus.Instance;
            
            _authenticatedPeers = new List<GrpcPeer>();
        }
        
        public async Task StartAsync()
        {
            // todo inject block service
            var p = new GrpcPeerService(this, _blockService);
            p.EventBus = EventBus;
            
            p.PeerSentDisconnection += POnPeerSentDisconnection;
            
            _server = new Server {
                Services = { PeerService.BindService(p) },
                Ports = { new ServerPort(IPAddress.Any.ToString(), _networkOptions.ListeningPort, ServerCredentials.Insecure) }
            };
            
            await Task.Run(() => _server.Start());
            
            // Add the provided boot nodes
            if (_networkOptions.BootNodes != null && _networkOptions.BootNodes.Any())
            {
                List<Task<bool>> taskList = _networkOptions.BootNodes.Select(Dial).ToList();
                await Task.WhenAll(taskList.ToArray<Task>());
            }
            else
            {
                Logger.LogWarning("Boot nodes list is empty.");
            }
        }

        private void POnPeerSentDisconnection(object sender, EventArgs e)
        {
            if (e is PeerDcEventArgs ea)
            {
                _authenticatedPeers.RemoveAll(p => p.RemoteEndpoint == ea.Peer);
            }
        }

        private async Task<bool> Dial(string address)
        {
            try
            {
                Logger.LogTrace($"Attempting to reach {address}.");
                
                var splitAddress = address.Split(":");
                Channel channel = new Channel(splitAddress[0], int.Parse(splitAddress[1]), ChannelCredentials.Insecure);
                        
                var client = new PeerService.PeerServiceClient(channel);
                var hsk = BuildHandshake();
                        
                var resp = await client.ConnectAsync(hsk, new CallOptions().WithDeadline(DateTime.UtcNow.AddSeconds(2)));

                if (resp.Success != true)
                    return false;

                _authenticatedPeers.Add(new GrpcPeer(channel, client, address, resp.Port));
                        
                Logger.LogTrace($"Connected to {address}.");

                return true;
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error while connection to {address}.");
                return false;
            }
        }

        public async Task StopAsync()
        {
            await _server.KillAsync();
            
            foreach (var peer in _authenticatedPeers)
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

        public async Task<bool> AddPeerAsync(string address)
        {
            return await Dial(address);
        }

        public async Task<bool> RemovePeerAsync(string address)
        {
            GrpcPeer peer = _authenticatedPeers.FirstOrDefault(p => p.PeerAddress == address);
            
            if (peer == null)
            {
                Logger?.LogWarning($"Could not find peer {address}.");
                return false;
            }

            await peer.SendDisconnectAsync();
            await peer.StopAsync();
            
            return _authenticatedPeers.Remove(peer);
        }

        public List<GrpcPeer> GetPeers()
        {
            return _authenticatedPeers.ToList();
        }

        public string GetPeer(string address)
        {
            return _authenticatedPeers.FirstOrDefault(p => p.PeerAddress == address)?.PeerAddress;
        }

        public bool AuthenticatePeer(string peer, Handshake handshake)
        {
            // todo verify use _accountService
            return true;
        }

        public bool FinalizeAuth(GrpcPeer peer)
        {
            _authenticatedPeers.Add(peer);
            return true;
        }

        public bool IsAuthenticated(string peer)
        {
            throw new NotImplementedException();
        }

        public Handshake GetHandshake()
        {
            return BuildHandshake();
        }
        
        private Handshake BuildHandshake()
        {
            var nd = new HandshakeData
            {
                ListeningPort = _networkOptions.ListeningPort,
                PublicKey = ByteString.CopyFrom(_accountService.GetPublicKeyAsync().Result),
                Version = GlobalConfig.ProtocolVersion,
            };
            
            byte[] sig = _accountService.SignAsync(SHA256.Create().ComputeHash(nd.ToByteArray())).Result;

            var hsk = new Handshake
            {
                HskData = nd,
                Sig = ByteString.CopyFrom(sig)
            };

            return hsk;
        }
    }
}