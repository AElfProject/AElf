using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Miner.EventMessages;
using AElf.Network;
using AElf.Network.Data;
using AElf.Node.Protocol.Protobuf.Generated;
using Easy.MessageHub;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Handshake = AElf.Node.Protocol.Protobuf.Generated.Handshake;

namespace AElf.Node.Protocol
{
    public class GrpcNetworkManager : INetworkManager, ISingletonDependency
    {
        public ILogger<GrpcNetworkManager> Logger { get; set; }
        
        private List<PeerService.PeerServiceClient> _channels;
        
        private readonly NetworkOptions _networkOptions;
        private ECKeyPair _nodeKey;

        private Server _server;
        
        public GrpcNetworkManager(IOptionsSnapshot<NetworkOptions> options)
        {
            Logger = NullLogger<GrpcNetworkManager>.Instance;
            
            _networkOptions = options.Value;
            _channels = new List<PeerService.PeerServiceClient>();
            _nodeKey = NodeConfig.Instance.ECKeyPair;
        }
        
        public async Task Start()
        {
            var p = new GrpcServerService(Logger);
            p.PeerAdded += POnPeerAdded;
            p.SetHandshakeProvider(BuildHandshake);
            
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

        private async Task Dial(string address)
        {
            try
            {
                Logger.LogTrace($"Attempting to reach {address}.");
                        
                NodeData nd = NodeData.FromString(address); // todo replace
                Channel channel = new Channel(nd.IpAddress, nd.Port, ChannelCredentials.Insecure);
                        
                var client = new PeerService.PeerServiceClient(channel);
                var hsk = BuildHandshake();
                        
                var resp = await client.ConnectAsync(hsk);

                _channels.Add(client);
                        
                Logger.LogTrace($"Connected to {address}.");
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Error while connection to {address}.");
            }
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

        public async Task BroadcastTx(AElf.Kernel.Transaction tx)
        {
            foreach (var client in _channels)
            {
                await client.SendTransactionAsync(tx);
            }
        }

        public Task Stop()
        {
            _server.KillAsync();
            return Task.FromResult(true);
        }
    }
}