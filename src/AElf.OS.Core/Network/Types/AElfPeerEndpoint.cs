using System.Net;
using System.Net.Sockets;

namespace AElf.OS.Network.Types
{
    // TODO: Which one is correct, Endpoint or EndPoint?
    public class AElfPeerEndpoint : DnsEndPoint
    {
        public AElfPeerEndpoint(string host, int port)
            : base(host, port, AddressFamily.Unspecified)
        {
        }

        public override string ToString()
        {
            return Host + ":" + Port;
        }
    }
}