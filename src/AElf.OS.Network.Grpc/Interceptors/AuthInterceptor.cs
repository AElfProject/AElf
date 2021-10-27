using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.OS.Network.Grpc
{
    public class AuthInterceptor : Interceptor
    {
        private readonly IPeerPool _peerPool;
        
        public ILogger<AuthInterceptor> Logger { get; set; }

        public AuthInterceptor(IPeerPool peerPool)
        {
            _peerPool = peerPool;
            Logger = NullLogger<AuthInterceptor>.Instance;
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request,
            ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                if (IsNeedAuth(context.Method))
                {
                    var peer = _peerPool.FindPeerByPublicKey(context.GetPublicKey());

                    if (peer == null)
                    {
                        Logger.LogWarning($"Could not find peer {context.GetPublicKey()}");
                        return Task.FromResult<TResponse>(null);
                    }

                    // check that the peers session is equal to one announced in the headers
                    var sessionId = context.GetSessionId();

                    if (!peer.InboundSessionId.BytesEqual(sessionId))
                    {
                        if (peer.InboundSessionId == null)
                        {
                            Logger.LogWarning($"Wrong inbound session id {context.Peer}, {context.Method}");
                            return Task.FromResult<TResponse>(null);
                        }

                        if (sessionId == null)
                        {
                            Logger.LogWarning($"Wrong context session id {context.Peer}, {context.Method}, {peer}");
                            return Task.FromResult<TResponse>(null);
                        }

                        Logger.LogWarning(
                            $"Unequal session id, {context.Peer} ({peer.InboundSessionId.ToHex()} vs {sessionId.ToHex()}) {context.GetPublicKey()}");
                        return Task.FromResult<TResponse>(null);
                    }

                    context.RequestHeaders.Add(new Metadata.Entry(GrpcConstants.PeerInfoMetadataKey, $"{peer}"));
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Auth interceptor error {context.Peer}, {context.Method}: ");
                throw;
            }

            return continuation(request, context);
        }

        private bool IsNeedAuth(string methodName)
        {
            return methodName != GetFullMethodName(nameof(PeerService.PeerServiceBase.Ping)) &&
                   methodName != GetFullMethodName(nameof(PeerService.PeerServiceBase.DoHandshake));
        }

        private string GetFullMethodName(string methodName)
        {
            return "/" + nameof(PeerService) + "/" + methodName;
        }

        public override Task<TResponse> ClientStreamingServerHandler<TRequest, TResponse>(IAsyncStreamReader<TRequest> requestStream, ServerCallContext context,
            ClientStreamingServerMethod<TRequest, TResponse> continuation)
        {
            try
            {
                var peer = _peerPool.FindPeerByPublicKey(context.GetPublicKey());

                if (peer == null)
                {
                    Logger.LogWarning($"Could not find peer {context.GetPublicKey()}");
                    return Task.FromResult<TResponse>(null);
                }
            
                if (!peer.InboundSessionId.BytesEqual(context.GetSessionId()))
                {
                    Logger.LogWarning($"Wrong session id, ({peer.InboundSessionId.ToHex()} vs {context.GetSessionId().ToHex()}) {context.GetPublicKey()}");
                    return Task.FromResult<TResponse>(null);
                }
        
                context.RequestHeaders.Add(new Metadata.Entry(GrpcConstants.PeerInfoMetadataKey, $"{peer}"));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Auth stream interceptor error: ");
                return null;
            }
            
            return continuation(requestStream, context);
        }
    }
}