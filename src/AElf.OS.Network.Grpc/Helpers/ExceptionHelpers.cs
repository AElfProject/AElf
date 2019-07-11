using System;
using System.Threading.Tasks;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public static class ExceptionHelpers
    {
        public static async Task CleanupAndThrowAsync(string exceptionMessage, Channel channel, Exception inner = null)
        {
            await channel.ShutdownAsync();
            throw new PeerDialException(exceptionMessage, inner);
        }
    }
}