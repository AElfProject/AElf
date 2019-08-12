using System;
using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc
{
    public class AuthInterceptor : Interceptor
    {
        private readonly IPeerPool _peerPool;
        
        public ILogger<AuthInterceptor> Logger { get; set; }

        public AuthInterceptor(IPeerPool peerPool)
        {
            _peerPool = peerPool;
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            if (context.Method != GetFullMethodName(nameof(PeerService.PeerServiceBase.Connect)))
            {
                // a method other that Connect is being called
                
                var peer = _peerPool.FindPeerByPublicKey(context.GetPublicKey());

                if (peer == null && context.Method != GetFullMethodName(nameof(PeerService.PeerServiceBase.Ping)))
                {
                    Logger.LogWarning($"Could not find peer {context.GetPublicKey()}");
                    return Task.FromResult<TResponse>(null);
                }

                // check that the peers session is equal to one announced in the headers
                if (peer != null && !peer.InboundSessionId.BytesEqual(context.GetSessionId()))
                {
                    Logger.LogWarning($"Wrong session id, ({peer.InboundSessionId.ToHex()} vs {context.GetSessionId().ToHex()}) {context.GetPublicKey()}");
                    return Task.FromResult<TResponse>(null);
                }
                
                context.RequestHeaders.Add(new Metadata.Entry(GrpcConstants.PeerInfoMetadataKey, $"{peer}"));
            }
            
            return continuation(request, context);
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
                Logger.LogError("Auth interceptor error: ", e);
                return null;
            }
            
            return continuation(requestStream, context);
        }
    }
}