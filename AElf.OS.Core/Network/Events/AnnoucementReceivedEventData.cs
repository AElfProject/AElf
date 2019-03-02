using AElf.Kernel;

namespace AElf.OS.Network.Events
{
    public class AnnoucementReceivedEventData
    {
        public int ChainId { get; private set; }
        public PeerNewBlockAnnouncement Announce { get; private set; }
        public string Peer { get; private set; }
        
        public AnnoucementReceivedEventData(int chainId, PeerNewBlockAnnouncement an, string peer)
        {
            Peer = peer;
            Announce = an;
            ChainId = chainId;
        }
    }
}