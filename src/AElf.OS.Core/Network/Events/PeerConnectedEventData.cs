using AElf.Kernel;

namespace AElf.OS.Network.Events
{
    public class PeerConnectedEventData
    {
        public BlockHeader Header { get; set; }
        public string Peer { get; set; }
    }
}