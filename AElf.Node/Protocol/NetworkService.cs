using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Miner.EventMessages;
using AElf.Network;
using AElf.Network.Data;
using AElf.Network.Peers;
using AElf.Node.Protocol.Protobuf.Generated;
using Easy.MessageHub;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using BlockRequest = AElf.Node.Protocol.Protobuf.Generated.BlockRequest;
using Handshake = AElf.Node.Protocol.Protobuf.Generated.Handshake;
using IPAddress = System.Net.IPAddress;

namespace AElf.Node.Protocol
{
    public class NetworkService : INetworkService, ISingletonDependency
    {
        public ILogger<NetworkService> Logger { get; set; }
        
        private List<Protobuf.Generated.PeerService.PeerServiceClient> _channels;
        
        private readonly NetworkOptions _networkOptions;
        private ECKeyPair _nodeKey;
        
        public NetworkService(IOptionsSnapshot<NetworkOptions> options)
        {
            Logger = NullLogger<NetworkService>.Instance;
            
            _networkOptions = options.Value;
            _channels = new List<Protobuf.Generated.PeerService.PeerServiceClient>();
            _nodeKey = NodeConfig.Instance.ECKeyPair;
        }
        
        public async Task Start()
        {
            var p = new PeerService(Logger);
            p.PeerAdded += POnPeerAdded;
            p.SetHandshakeProvider(BuildHandshake);
            
            Server server = new Server
            {
                Services = { Protobuf.Generated.PeerService.BindService(p) },
                Ports = { new ServerPort(IPAddress.Any.ToString(), _networkOptions.ListeningPort, ServerCredentials.Insecure) }
            };
            
            server.Start();
            
            // Add the provided boot nodes
            if (_networkOptions.BootNodes != null && _networkOptions.BootNodes.Any())
            {
                foreach (var btn in _networkOptions.BootNodes)
                {
                    try
                    {
                        Logger.LogTrace($"Attempting to reach {btn}.");
                        
                        NodeData nd = NodeData.FromString(btn); // todo replace
                        Channel channel = new Channel(nd.IpAddress, nd.Port, ChannelCredentials.Insecure);
                        
                        var client = new Protobuf.Generated.PeerService.PeerServiceClient(channel);
                        var hsk = BuildHandshake();
                        
                        var resp = await client.ConnectAsync(hsk);

                        _channels.Add(client);
                        
                        Logger.LogTrace($"Connected to {btn}.");
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, $"Error while connection to {btn}.");
                    }
                }
            }
            else
            {
                Logger.LogWarning("Boot nodes list is empty.");
            }
            
            MessageHub.Instance.Subscribe<BlockMined>(async inBlock =>
            {
                if (inBlock?.Block == null)
                {
                    Logger.LogWarning("[event] Block null.");
                    return;
                }

                byte[] blockHash = inBlock.Block.GetHash().DumpByteArray();

                await BroadcastAnnounce(new Announcement {Id = ByteString.CopyFrom(blockHash)});
            });
        }

        private void POnPeerAdded(object sender, EventArgs e)
        {
            if (e is PeerAddedEventArgs pe)
            {
                _channels.Add(pe.Client);
            }
        }

        private Handshake BuildHandshake()
        {
            var nd = new HandshakeData
            {
                ListeningPort = _networkOptions.ListeningPort,
                PublicKey = ByteString.CopyFrom(_nodeKey.PublicKey),
                Version = GlobalConfig.ProtocolVersion,
            };
            
            ECSigner signer = new ECSigner();
            ECSignature sig = signer.Sign(_nodeKey, SHA256.Create().ComputeHash(nd.ToByteArray()));

            var hsk = new Handshake
            {
                HskData = nd,
                Sig = ByteString.CopyFrom(sig.SigBytes)
            };

            return hsk;
        }

        public async Task BroadcastAnnounce(Announcement an)
        {
            foreach (var client in _channels)
            {
                await client.AnnounceAsync(an);
            }
        }

        public Task Stop()
        {
            return Task.FromResult(true);
        }
    }

    public class PeerAddedEventArgs : EventArgs
    {
        public Protobuf.Generated.PeerService.PeerServiceClient Client;
    }
    
    public class PeerService : Protobuf.Generated.PeerService.PeerServiceBase
    {
        public event EventHandler PeerAdded;
        
        private readonly int _localPort;
        public ILogger<NetworkService> Logger { get; set; }
        private Protobuf.Generated.PeerService.PeerServiceClient client;

        private Func<Handshake> _handshakeProvider;
        
        public PeerService(ILogger<NetworkService> logger)
        {
            Logger = logger;
        }

        public void SetHandshakeProvider(Func<Handshake> handshakeProvider)
        {
            _handshakeProvider = handshakeProvider;
        }

        /// <summary>
        /// First step of the connect/auth process.Used to initiate a connection. The provided payload should be the
        /// clients authentication information. When receiving this call, protocol dictates you send the client your auth
        /// information. The response says whether or not you can connect.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<AuthResponse> Connect(Handshake request, ServerCallContext context)
        {
            Logger?.LogTrace($"[{context.Peer}] has initiated a connection request.");
            
            try
            {
                var peerServer = context.Peer.Split(":")[1] + ":" + request.HskData.ListeningPort;
                Logger?.LogDebug($"Attempting connect to {peerServer}");
                
                Channel channel = new Channel(peerServer, ChannelCredentials.Insecure);
                client = new Protobuf.Generated.PeerService.PeerServiceClient(channel);
            
                // todo verify 
                
                // send our credentials
                var hsk = _handshakeProvider();
                var resp = client.Authentify(hsk);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error during connect.");
                return Task.FromResult(new AuthResponse { Err = AuthError.UnknownError});
            }
            
            // todo if resp ok
            PeerAdded?.Invoke(this, new PeerAddedEventArgs { Client = client });
            
            return Task.FromResult(new AuthResponse { Success = true});
        }

        /// <summary>
        /// Second step of the connect/auth process. This takes place after the connect to receive the peers
        /// information and on return let him know that we've validated.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<AuthResponse> Authentify(Handshake request, ServerCallContext context)
        {
            Logger.LogTrace($"[{context.Peer}] Is calling back with his auth.");
            
            try
            {
                // todo verify auth
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error during connect.");
                return Task.FromResult(new AuthResponse { Err = AuthError.UnknownError});
            }
            
            return Task.FromResult(new AuthResponse { Success = true});
        }

        /// <summary>
        /// Call back for when a Peer has broadcast an announcement.
        /// </summary>
        /// <param name="an"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override Task<VoidReply> Announce(Announcement an, ServerCallContext context)
        {
            Logger.LogTrace($"Received announce {an.Id.ToByteArray().ToHex()} from {context.Peer}.");
            
            try
            {
                // todo handle
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
            return Task.FromResult(new VoidReply());
        }

        public override Task<BlockReply> RequestBlock(BlockRequest request, ServerCallContext context)
        {
            return Task.Run(async () =>  {
                // await Task.Delay(TimeSpan.FromSeconds(3)); // execute logic
                // Console.WriteLine($"{DateTime.Now} Request from {context.Peer}");
                return new BlockReply {Message = "{ number: " + request.BlockNumber + " }"};
            });
        }
    }
}