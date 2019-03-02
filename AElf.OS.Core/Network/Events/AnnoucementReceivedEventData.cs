using AElf.Kernel;

namespace AElf.OS.Network.Events
{
    public class AnnoucementReceivedEventData
    {
        public PeerNewBlockAnnouncement Announce { get; private set; }
        public string Peer { get; private set; }
        
        public AnnoucementReceivedEventData(PeerNewBlockAnnouncement an, string peer)
        {
            Peer = peer;
            Announce = an;
        }
    }
}