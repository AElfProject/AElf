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

            Metadata.Entry metadataEntry = null;
            if (context.Options.Headers != null && context.Options.Headers.Any())
            {
                metadataEntry = context.Options.Headers.FirstOrDefault(m => 
                    String.Equals(m.Key, GrpcConsts.MetricInfoMetadataKey, StringComparison.Ordinal));
            }

            if (metadataEntry != null)
                context.Options.Headers.Remove(metadataEntry);

            string metricInfo = metadataEntry?.Value ?? DefaultMetricInfoString; 

            async Task<TResponse> RetryCallback(Task<TResponse> responseTask)
            {
                var response = responseTask;

                // if no problem occured return
                if (!response.IsFaulted)
                {
                    Logger.LogDebug($"[{PeerIp}] {metricInfo} - Success");

                    return response.Result;
                }

                // if a problem occured but reached the max retries
                if (retryCount == _retryCount)
                {
                    Logger.LogDebug($"[{PeerIp}] {metricInfo} - Last retry");

                    return response.Result;
                }
                
                retryCount++;
                
                Logger.LogDebug($"[{PeerIp}] {metricInfo} - Retrying");

                // try again
                var result = continuation(request, context).ResponseAsync.ContinueWith(RetryCallback).Unwrap();
                return await result;
            }

            var responseContinuation = continuation(request, context);
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