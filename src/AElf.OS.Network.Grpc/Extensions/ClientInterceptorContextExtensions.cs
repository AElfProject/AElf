using System;
using System.Linq;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network.Grpc
{
    public static class ClientInterceptorContextExtensions
    {
        /// <summary>
        /// Returns the header with the given key. If the key is not found or there's no headers, returns null.
        /// </summary>
        public static string GetHeaderStringValue<TRequest, TResponse>(this ClientInterceptorContext<TRequest, TResponse> context, string key) 
            where TRequest : class 
            where TResponse : class
        {
            if (!AnyHeaders(context))
                return null;

            return context.Options.Headers.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.Ordinal))?.Value;
        }

        /// <summary>
        /// Returns true if any headers are available, false otherwise.
        /// </summary>
        public static bool AnyHeaders<TRequest, TResponse>(ClientInterceptorContext<TRequest, TResponse> context)
            where TRequest : class 
            where TResponse : class
        {
            return context.Options.Headers != null && context.Options.Headers.Any();
        }
    }
}