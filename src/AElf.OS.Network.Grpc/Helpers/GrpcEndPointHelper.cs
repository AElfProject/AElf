using System.Net;
using AElf.OS.Network.Helpers;

namespace AElf.OS.Network.Grpc.Helpers
{
    public static class GrpcEndPointHelper
    {
        public static bool ParseDnsEndPoint(string grpcEndpoint, out DnsEndPoint outEndPoint)
        {
            outEndPoint = null;

            var splitRes = grpcEndpoint.Split(':');
            var nonPrefixedGrpcEndpoint = splitRes[1] + ":" + splitRes[2];

            if (!AElfPeerEndpointHelper.TryParse(nonPrefixedGrpcEndpoint, out DnsEndPoint parsedDnsEndpoint))
                return false;

            outEndPoint = parsedDnsEndpoint;

            return true;
        }
    }
}