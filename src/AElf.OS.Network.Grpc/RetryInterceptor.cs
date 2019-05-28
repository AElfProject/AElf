using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network.Grpc
{
    public class RetryInterceptor : Interceptor
    {
        private int _retryCount = NetworkConsts.DefaultMaxRequestRetryCount;

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var retryCount = 0;

            async Task<TResponse> RetryCallback(Task<TResponse> responseTask)
            {
                var response = responseTask;

                // if no problem occured return
                if (!response.IsFaulted)
                {
                    return response.Result;
                }

                // if a problem occured but reached the max retries
                if (retryCount == _retryCount)
                {
                    return response.Result;
                }
                
                retryCount++;

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