using System.Net;

namespace AElf.OS.Network.Helpers
{
    public static class IpEndPointHelper
    {
        public static IPEndPoint Parse(string endpointString)
        {
            var split = endpointString.Split(':');
            return new IPEndPoint(IPAddress.Parse(split[0]), int.Parse(split[1]));
        }
        
        public static bool TryParse(string endpointString, out IPEndPoint endpoint)
        {
            endpoint = null;
            
            if (string.IsNullOrEmpty(endpointString) || endpointString.Trim().Length == 0)
                return false;

            var split = endpointString.Split(':');
            
            if (!IPAddress.TryParse(split[0], out IPAddress parsedAddress))
                return false;

            if (!TryParsePort(split[1], out int parsedPort))
                return false;
            
            endpoint = new IPEndPoint(parsedAddress, parsedPort);
            
            return true;
        }

        private static bool TryParsePort(string portString, out int port)
        {
            port = -1;

            if (!int.TryParse(portString, out port) || port < IPEndPoint.MinPort || port > IPEndPoint.MaxPort)
                return false;

            return true;
        }
    }
}