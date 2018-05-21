using System;

namespace AElf.Kernel.Node.Network.Peers
{
    public class Peer : IPeer
    {
        public string IpAddress { get; set; }
        public UInt16 Port { get; set; }

        public Peer(string ipAddress, UInt16 port)
        {
            IpAddress = ipAddress;
            Port = port;
        }
    }
}