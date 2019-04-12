using System.Threading.Tasks;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network.Grpc
{
    public class AuthInterceptor : Interceptor
    {
        private readonly IPeerPool _peerPool;

        public AuthInterceptor(IPeerPool peerPool)
        {
            _peerPool = peerPool;
        }

        public override Task<TResponse> UnaryServerHandler<TRequest, TResponse>(TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
        {
            if (context.Method != "/" + nameof(PeerService) + "/" + nameof(PeerService.PeerServiceBase.Connect))
            {
                var peer = _peerPool.FindPeerByPublicKey(context.GetPublicKey());

                if (peer == null)
                    return Task.FromResult<TResponse>(null);
                
                context.RequestHeaders.Add(new Metadata.Entry(GrpcConsts.PeerInfoMetadataKey, $"{peer}"));
            }
            
            return continuation(request, context);
        }
    }
}