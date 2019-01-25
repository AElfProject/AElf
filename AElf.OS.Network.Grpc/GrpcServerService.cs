using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.OS.Network.Events;
using AElf.OS.Network.Temp;
using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Volo.Abp.EventBus.Local;

namespace AElf.OS.Network.Grpc
{
    public class GrpcServerService : PeerService.PeerServiceBase
    {
        private readonly IPeerAuthentificator _peerAuthenticator;
        private readonly IBlockService _blockService;
        private readonly ILocalEventBus _localEventBus;

        public ILogger<GrpcNetworkManager> Logger { get; set; }
        private PeerService.PeerServiceClient client;
        
        public GrpcServerService(ILogger<GrpcNetworkManager> logger, IPeerAuthentificator peerAuthenticator,
            IBlockService blockService, ILocalEventBus localEventBus)
        {
            _peerAuthenticator = peerAuthenticator;
            _blockService = blockService;
            _localEventBus = localEventBus;
            Logger = logger;
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

                if (channel.State != ChannelState.Ready)
                {
                    var c = channel.WaitForStateChangedAsync(channel.State);
                }

                bool isAuth = _peerAuthenticator.AuthenticatePeer(peerServer, request);
                
                // send our credentials
                var hsk = _peerAuthenticator.GetHandshake();
                var resp = client.Authentify(hsk);
                
                // If auth ok -> finalize
                _peerAuthenticator.FinalizeAuth(new GrpcPeer(channel, client, peerServer));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error during connect.");
                return Task.FromResult(new AuthResponse { Err = AuthError.UnknownError});
            }
            
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
                var peerServer = context.Peer.Split(":")[1] + ":" + request.HskData.ListeningPort;
                bool isAuth = _peerAuthenticator.AuthenticatePeer(peerServer, request);
                // todo verify auth
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error during connect.");
                return Task.FromResult(new AuthResponse { Err = AuthError.UnknownError});
            }
            
            return Task.FromResult(new AuthResponse { Success = true});
        }

        public override Task<VoidReply> SendTransaction(Transaction tx, ServerCallContext context)
        {
            try
            {
                _localEventBus.PublishAsync(new TxReceivedEventData(tx));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error during connect.");
            }
            
            return Task.FromResult(new VoidReply());
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
                _localEventBus.PublishAsync(new AnnoucementReceivedEventData(Hash.LoadByteArray(an.Id.ToByteArray())));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error during announcement handle.");
            }
            
            return Task.FromResult(new VoidReply());
        }

        public override Task<BlockReply> RequestBlock(BlockRequest request, ServerCallContext context)
        {
            try
            {
                Block block = _blockService.GetBlockAsync(Hash.LoadByteArray(request.Id.ToByteArray())).Result;
                byte[] s = block.ToByteArray();
                
                return Task.FromResult(new BlockReply { Block = block });
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error during RequestBlock handle.");
            }
            
            return Task.FromResult(new BlockReply());
        }
    }
}