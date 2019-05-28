using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc
{
    public class RetryInterceptor : Interceptor
    {
        private int _retryCount = NetworkConsts.DefaultMaxRequestRetryCount;
        
        public ILogger Logger { get; set; }
        public string PeerIp { get; set; }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var retryCount = 0;
            
            Logger.LogDebug($"[{PeerIp}] {context.Method.Name} - Current time {DateTime.Now}");

            async Task<TResponse> RetryCallback(Task<TResponse> responseTask)
            {
                var response = responseTask;

                // if no problem occured return
                if (!response.IsFaulted)
                {
                    return response.Result;
                }

                retryCount++;

                Logger.LogDebug($"[{PeerIp}] {context.Method.Name} RETRY retry count {retryCount}");

                // if a problem occured but reached the max retries
                if (retryCount == _retryCount)
                {
                    Logger.LogDebug($"[{PeerIp}] {context.Method.Name} RETRY not retrying");
                    return response.Result;
                }

                // try again
                var result = continuation(request, context).ResponseAsync.ContinueWith(RetryCallback).Unwrap();
                return result.Result;
            }

            var responseContinuation = continuation(request, context);
            var responseAsync = responseContinuation.ResponseAsync.ContinueWith(RetryCallback);

            return new AsyncUnaryCall<TResponse>(
                responseAsync.Result,
                responseContinuation.ResponseHeadersAsync,
                responseContinuation.GetStatus,
                responseContinuation.GetTrailers,
                responseContinuation.Dispose);
        }
    }
}