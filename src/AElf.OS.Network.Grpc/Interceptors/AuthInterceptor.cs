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
                var peer = _peerPool.FindPeerByPublicKey(context.GetPublicKey());

                if (peer == null && context.Method != GetFullMethodName(nameof(PeerService.PeerServiceBase.Ping)))
                {
                    Logger.LogWarning($"Could not find peer {context.GetPublicKey()}");
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
            var peer = _peerPool.FindPeerByPublicKey(context.GetPublicKey());

            if (peer == null)
            {
                Logger.LogWarning($"Could not find peer {context.GetPublicKey()}");
                return Task.FromResult<TResponse>(null);
            }
        
            context.RequestHeaders.Add(new Metadata.Entry(GrpcConstants.PeerInfoMetadataKey, $"{peer}"));
            
            return continuation(requestStream, context);
        }
    }
}