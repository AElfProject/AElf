namespace AElf.Network.Sim.Tests
{
    public class NetEvent
    {
        public int ActionId { get; set; }
        public Peer Peer { get; set; }
    }

    public class Peer
    {
        public string IpAddress { get; set; }
        public int Port { get; set; }
    }
}