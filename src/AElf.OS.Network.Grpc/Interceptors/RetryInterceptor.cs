using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network.Grpc
{
    public class RetryInterceptor : Interceptor
    {
        private int _retryCount = NetworkConstants.DefaultMaxRequestRetryCount;

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var retryCount = 0;

            Metadata.Entry timeoutInMilliSecondsMetadataEntry = null;
            if (context.Options.Headers != null && context.Options.Headers.Any())
            {
                timeoutInMilliSecondsMetadataEntry = context.Options.Headers.FirstOrDefault(m => 
                    string.Equals(m.Key, GrpcConstants.TimeoutMetadataKey, StringComparison.Ordinal));
            }

            var timeoutSpan = timeoutInMilliSecondsMetadataEntry == null
                ? TimeSpan.FromMilliseconds(GrpcConstants.DefaultRequestTimeoutInMilliSeconds)
                : TimeSpan.FromMilliseconds(int.Parse(timeoutInMilliSecondsMetadataEntry.Value)); 
            
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
                var retryContext = BuildNewContext(context, timeoutSpan);
                var result = continuation(request, retryContext).ResponseAsync.ContinueWith(RetryCallback).Unwrap();
                
                return await result;
            }

            var newContext = BuildNewContext(context, timeoutSpan);
            var responseContinuation = continuation(request, newContext);
            
            var responseAsync = responseContinuation.ResponseAsync.ContinueWith(RetryCallback).Unwrap();

            return new AsyncUnaryCall<TResponse>(
                responseAsync,
                responseContinuation.ResponseHeadersAsync,
                responseContinuation.GetStatus,
                responseContinuation.GetTrailers,
                responseContinuation.Dispose);
        }

        private ClientInterceptorContext<TRequest, TResponse> BuildNewContext<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> oldContext, TimeSpan timeout)  
            where TRequest : class
            where TResponse : class
        {
            return new ClientInterceptorContext<TRequest, TResponse>(oldContext.Method, oldContext.Host,
                oldContext.Options.WithDeadline(DateTime.UtcNow.Add(timeout)));
        }
    }
}