using System.Collections.Generic;

namespace AElf.Management.Models
{
    public class PeerResult
    {
        public int Auth { get; set; }

        public List<Peer> Peers { get; set; }
    }

    public class Peer
    {
        public Address Address { get; set; }

        public bool IsBp { get; set; }
    }

    public class Address
    {
        public string IpAddress { get; set; }

        public int Port { get; set; }
    }
}