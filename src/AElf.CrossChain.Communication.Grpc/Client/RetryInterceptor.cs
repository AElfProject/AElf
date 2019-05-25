using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.CrossChain.Communication.Grpc
{
    public class RetryInterceptor : Interceptor
    {
        private readonly int _retryTimes;

        public RetryInterceptor(int retryTimes)
        {
            _retryTimes = retryTimes;
        }

        public override TResponse BlockingUnaryCall<TRequest, TResponse>(TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
            BlockingUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var retryCount = 0;

            while (true)
            {
                try
                {
                    var response = continuation(request, context);
                    return response;
                }
                catch (RpcException e)
                {
                    if (retryCount < _retryTimes)
                    {
                        Console.WriteLine($"Failed {retryCount} time(s)");
                        retryCount++;
                        continue;
                    }

                    throw e;
                }
            }
        }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var retryCount = 0;

            async Task<TResponse> RetryCallback(Task<TResponse> responseTask)
            {
                var response = responseTask;
                if (!response.IsFaulted)
                {
                    return response.Result;
                }

                retryCount++;

                if (retryCount == _retryTimes)
                {
                    return response.Result;
                }

                Console.WriteLine($"Failed {retryCount} time(s)");
                await Task.Delay(1000).ConfigureAwait(false);
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

        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context, AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return continuation(request, context);
        }
    }
}