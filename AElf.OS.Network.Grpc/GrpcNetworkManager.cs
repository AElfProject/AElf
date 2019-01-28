using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network.Grpc.Events;
using AElf.OS.Network.Temp;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc
{
    public class GrpcNetworkManager : INetworkManager, IPeerAuthentificator, ISingletonDependency
    {
        private readonly IAccountService _accountService;
        private readonly IBlockService _blockService;
        private readonly ILocalEventBus _localEventBus;
        
        public ILogger<GrpcNetworkManager> Logger { get; set; }
                
        private readonly NetworkOptions _networkOptions;

        private Server _server;

        private List<GrpcPeer> _authenticatedPeers;
        
        public GrpcNetworkManager(IOptionsSnapshot<NetworkOptions> options, 
            IAccountService accountService, IBlockService blockService, ILocalEventBus localEventBus)
        {
            _accountService = accountService;
            _blockService = blockService;
            Logger = NullLogger<GrpcNetworkManager>.Instance;
            
             _localEventBus = localEventBus;
            
            _authenticatedPeers = new List<GrpcPeer>();
            
            _networkOptions = options.Value;
        }
        
        public async Task StartAsync()
        {
            // todo inject block service
            var p = new GrpcServerService(Logger, this, _blockService, _localEventBus);
            
            p.PeerSentDisconnection += POnPeerSentDisconnection;
            
            _server = new Server {
                Services = { PeerService.BindService(p) },
                Ports = { new ServerPort(IPAddress.Any.ToString(), _networkOptions.ListeningPort, ServerCredentials.Insecure) }
            };
            
            _server.Start();
            
            // Add the provided boot nodes
            if (_networkOptions.BootNodes != null && _networkOptions.BootNodes.Any())
            {
                foreach (var btn in _networkOptions.BootNodes)
                {
                    await Dial(btn);
                }
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
                
                var splitted = address.Split(":");
                Channel channel = new Channel(splitted[0], int.Parse(splitted[1]), ChannelCredentials.Insecure);
                        
                var client = new PeerService.PeerServiceClient(channel);
                var hsk = BuildHandshake();
                        
                var resp = await client.ConnectAsync(hsk);

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

        private Handshake BuildHandshake()
        {
            var nd = new HandshakeData
            {
                ListeningPort = _networkOptions.ListeningPort,
                PublicKey = ByteString.CopyFrom(_accountService.GetPublicKey().Result),
                Version = GlobalConfig.ProtocolVersion,
            };
            
            byte[] sig = _accountService.Sign(SHA256.Create().ComputeHash(nd.ToByteArray())).Result;

            var hsk = new Handshake
            {
                HskData = nd,
                Sig = ByteString.CopyFrom(sig)
            };

            return hsk;
        }

        public async Task BroadcastAnnounce(Block b)
        {
            foreach (var peer in _authenticatedPeers)
            {
                try
                {
                    await peer.AnnounceAsync(new Announcement { Id = ByteString.CopyFrom(b.GetHashBytes()) });
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error while sending block."); // todo improve
                }
            }
        }

        public async Task BroadcastTransaction(Transaction tx)
        {
            foreach (var peer in _authenticatedPeers)
            {
                try
                {
                    await peer.SendTransactionAsync(tx);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error while sending transaction."); // todo improve
                }
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

        public async Task<IBlock> GetBlockByHash(Hash hash, string peer = null)
        {
            // todo use peer if specified
            foreach (var p in _authenticatedPeers)
            {
                try
                {
                    if (p == null)
                    {
                        Logger.LogWarning("No peers left.");
                        return null;
                    }
            
                    Logger.LogDebug($"Attempting get with {p}");

                    BlockReply block = await p.RequestBlockAsync(new BlockRequest { Id = hash.Value });

                    if (block.Block != null)
                        return block.Block;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error while requesting block.");
                }
            }

            return null;
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

        public List<string> GetPeers()
        {
            return _authenticatedPeers.Select(p => p.PeerAddress).ToList();
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
    }
}