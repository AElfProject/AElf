using System.Net;
using System.Net.Sockets;

namespace AElf.OS.Network.Types
{
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