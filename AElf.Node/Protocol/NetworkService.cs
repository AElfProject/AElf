using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Network;
using AElf.Network.Data;
using AElf.Node.Protocol.Protobuf.Generated;
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
        
        private List<Channel> _channels;
        
        private readonly NetworkOptions _networkOptions;
        private ECKeyPair _nodeKey;
        
        public NetworkService(IOptionsSnapshot<NetworkOptions> options)
        {
            Logger = NullLogger<NetworkService>.Instance;
            
            _networkOptions = options.Value;
            _channels = new List<Channel>();
            _nodeKey = NodeConfig.Instance.ECKeyPair;
        }
        
        public async Task Start()
        {
            var p = new PeerService(_networkOptions.ListeningPort);
            
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
                    NodeData nd = NodeData.FromString(btn); // todo replace
                    Channel channel = new Channel(nd.IpAddress, nd.Port, ChannelCredentials.Insecure);

                    try
                    {
                        var client = new Protobuf.Generated.PeerService.PeerServiceClient(channel);
                        var hsk = BuildHandshake();
                        
                        var resp = await client.AuthentifyAsync(hsk);
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

        public Task Stop()
        {
            return Task.FromResult(true);
        }
    }
    
    public class PeerService : Protobuf.Generated.PeerService.PeerServiceBase
    {
        private readonly int _localPort;
        public ILogger<PeerService> Logger { get; set; }
        private Protobuf.Generated.PeerService.PeerServiceClient client;
        
        public PeerService(int localPort)
        {
            _localPort = localPort;
            Logger = NullLogger<PeerService>.Instance;
        }

        public override Task<Handshake> Authentify(Handshake request, ServerCallContext context)
        {            
            var nodeKey = NodeConfig.Instance.ECKeyPair;
            
            var nd = new HandshakeData
            {
                ListeningPort = _localPort,
                PublicKey = ByteString.CopyFrom(nodeKey.PublicKey),
                Version = GlobalConfig.ProtocolVersion,
            };
            
            ECSigner signer = new ECSigner();
            ECSignature sig = signer.Sign(nodeKey, SHA256.Create().ComputeHash(nd.ToByteArray()));
            
            Channel channel = new Channel(context.Peer.Split(":")[1] + ":" + request.HskData.ListeningPort, ChannelCredentials.Insecure);
            client = new Protobuf.Generated.PeerService.PeerServiceClient(channel);
        }

        private async void RequestBlock(object o)
        {
            await client.RequestBlockAsync(new BlockRequest { BlockNumber = 5 });
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