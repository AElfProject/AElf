using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.OS.Network.Grpc.Generated;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc
{
    public class GrpcServerService : PeerService.PeerServiceBase
    {
        public event EventHandler PeerAdded;
        
        private readonly int _localPort;
        public ILogger<GrpcNetworkManager> Logger { get; set; }
        private PeerService.PeerServiceClient client;

        private Func<Handshake> _handshakeProvider;
        
        public GrpcServerService(ILogger<GrpcNetworkManager> logger)
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
                client = new PeerService.PeerServiceClient(channel);
            
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
                Logger.LogError(e, "Error during announcement handle.");
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