using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Account.Application;
using AElf.OS.Network.Infrastructure;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Grpc.Core.Utils;

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
            if (context.Method != "/PeerService/Connect")
            {
                var peer = _peerPool.FindPeerByPublicKey(context.RequestHeaders.First(entry => entry.Key == "public-key")
                    .Value);

                if (peer == null)
                    return Task.FromResult<TResponse>(null);
            }
            
            return continuation(request, context);
        }
    }
}