using System.Net;

namespace AElf.Kernel.Helper
{
    public static class UriHelper
    {
        /// <summary>
        /// Tries to parse a grpc URI. format: ipv4:127.0.0.1:8000 "
        /// </summary>
        /// <param name="url"></param>
        /// <param name="endPoint"></param>
        /// <returns></returns>
        public static bool TryParsePrefixedEndpoint(string url, out IPEndPoint endPoint)
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