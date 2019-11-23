using System;
using System.Linq;
using Grpc.Core;
using Grpc.Core.Interceptors;

namespace AElf.OS.Network.Grpc
{
    public static class ClientInterceptorContextExtensions
    {
        /// <summary>
        /// Returns the header with the given key. If the key is not found or there's no headers, returns null. If the
        /// header is found and the <see cref="clearHeader"/> parameter is true, the parameter will be removed from the
        /// headers.
        /// </summary>
        public static string GetHeaderStringValue<TRequest, TResponse>(
            this ClientInterceptorContext<TRequest, TResponse> context, string key, bool clearHeader = false) 
            where TRequest : class 
            where TResponse : class
        {
            if (!AnyHeaders(context))
                return null;

            Metadata.Entry valueItem = context.Options.Headers.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.Ordinal));

            if (clearHeader && valueItem != null)
                context.Options.Headers.Remove(valueItem);

            return valueItem?.Value;
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