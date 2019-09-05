using System.Linq;
using Grpc.Core;

namespace AElf.OS.Network.Grpc
{
    public static class CallContextExtensions
    {
        public static string GetPublicKey(this ServerCallContext context)
        {
            
            return context.RequestHeaders
                .FirstOrDefault(entry => entry.Key == GrpcConstants.PubkeyMetadataKey)?.Value;
        }
        
        public static byte[] GetSessionId(this ServerCallContext context)
        {
            return context.RequestHeaders
                .FirstOrDefault(entry => entry.Key == GrpcConstants.SessionIdMetadataKey)?.ValueBytes;
        }
        
        public static string GetPeerInfo(this ServerCallContext context)
        {
            return context.RequestHeaders
                .FirstOrDefault(entry => entry.Key == GrpcConstants.PeerInfoMetadataKey)?.Value;
        }
    }
}