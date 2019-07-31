using System.Net;

namespace AElf.OS.Network.Grpc.Helpers
{
    public static class GrpcUriHelpers
    {
        /// <summary>
        /// Tries to parse a grpc URI.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static bool TryParseGrpcUri(string url, out IPEndPoint endPoint)
        {
            endPoint = null;
            
            var splitRes = url.Split(':');

            if (splitRes.Length != 3)
                return false;

            if (!IPAddress.TryParse(splitRes[1], out IPAddress parsedAddress))
                return false;

            if (!int.TryParse(splitRes[2], out int parsedPort))
                return false;

            endPoint = new IPEndPoint(parsedAddress, parsedPort);

            return true;
        }
    }
}