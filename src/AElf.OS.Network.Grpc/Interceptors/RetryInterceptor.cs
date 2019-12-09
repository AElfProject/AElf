using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network.Grpc
{
    public class RetryInterceptor : Interceptor
    {
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var currentRetry = 0;

            string headerTimeout = context.GetHeaderStringValue(GrpcConstants.TimeoutMetadataKey, true);
            int timeout = headerTimeout == null ? GrpcConstants.DefaultRequestTimeout : int.Parse(headerTimeout);
            var timeoutSpan = TimeSpan.FromMilliseconds(timeout);

            string headerRetryCount = context.GetHeaderStringValue(GrpcConstants.RetryCountMetadataKey, true);
            int retryCount = headerRetryCount == null ? NetworkConstants.DefaultRequestRetryCount : int.Parse(headerRetryCount);

            async Task<TResponse> RetryCallback(Task<TResponse> responseTask)
            {
                var response = responseTask;

                // if no problem occured return
                if (!response.IsFaulted)
                {
                    return response.Result;
                }

                // if a problem occured but reached the max retries
                if (currentRetry == retryCount)
                {
                    return response.Result;
                }
                
                currentRetry++;
                
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