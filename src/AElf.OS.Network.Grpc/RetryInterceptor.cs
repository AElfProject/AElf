using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Network.Grpc
{
    public class RetryInterceptor : Interceptor
    {
        private const string DefaultMetricInfoString = "Unknown metadata";
        
        private int _retryCount = NetworkConsts.DefaultMaxRequestRetryCount;
        
        public ILogger Logger { get; set; }
        public string PeerIp { get; set; }

        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(TRequest request,
            ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            var retryCount = 0;

            Metadata.Entry metricInfoMetadataEntry = null;
            Metadata.Entry timeoutInMilliSecondsMetaEntry = null;
            if (context.Options.Headers != null && context.Options.Headers.Any())
            {
                metricInfoMetadataEntry = context.Options.Headers.FirstOrDefault(m => 
                    String.Equals(m.Key, GrpcConsts.MetricInfoMetadataKey, StringComparison.Ordinal));
                timeoutInMilliSecondsMetaEntry = context.Options.Headers.FirstOrDefault(m => 
                    String.Equals(m.Key, GrpcConsts.TimeoutMetadataKey, StringComparison.Ordinal));
            }

            if (metricInfoMetadataEntry != null)
                context.Options.Headers.Remove(metricInfoMetadataEntry);

            string metricInfo = metricInfoMetadataEntry?.Value ?? DefaultMetricInfoString;

            var timeoutSpan = timeoutInMilliSecondsMetaEntry == null
                ? TimeSpan.FromMilliseconds(GrpcConsts.DefaultRequestTimeoutInMilliSeconds)
                : TimeSpan.FromMilliseconds(int.Parse(timeoutInMilliSecondsMetaEntry.Value)); 
            
            async Task<TResponse> RetryCallback(Task<TResponse> responseTask)
            {
                var response = responseTask;

                // if no problem occured return
                if (!response.IsFaulted)
                {
                    Logger.LogDebug($"[{PeerIp}] {metricInfo} - succeed.");

                    return response.Result;
                }

                // if a problem occured but reached the max retries
                if (retryCount == _retryCount)
                {
                    Logger.LogDebug($"[{PeerIp}] {metricInfo} - retry finished.");

                    return response.Result;
                }
                
                retryCount++;
                
                Logger.LogDebug($"[{PeerIp}] {metricInfo} - Retrying");
                
                // try again

                var retryContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host,
                    context.Options.WithDeadline(DateTime.UtcNow.Add(timeoutSpan)));
                var result = continuation(request, retryContext).ResponseAsync.ContinueWith(RetryCallback).Unwrap();
                return await result;
            }

            var newContext = new ClientInterceptorContext<TRequest, TResponse>(context.Method, context.Host,
                context.Options.WithDeadline(DateTime.UtcNow.Add(timeoutSpan)));
            var responseContinuation = continuation(request, newContext);
            var responseAsync = responseContinuation.ResponseAsync.ContinueWith(RetryCallback).Unwrap();

            return new AsyncUnaryCall<TResponse>(
                responseAsync,
                responseContinuation.ResponseHeadersAsync,
                responseContinuation.GetStatus,
                responseContinuation.GetTrailers,
                responseContinuation.Dispose);
        }
    }
}