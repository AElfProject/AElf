using System;
using System.Linq;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public static class CallContextExtensions
    {
        public static string GetPublicKey(this ServerCallContext context)
        {
            try
            {
                return context.RequestHeaders.First(entry => entry.Key == GrpcConsts.PubkeyMetadataKey).Value;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }
    }
}