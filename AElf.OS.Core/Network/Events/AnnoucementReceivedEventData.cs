using AElf.Kernel;

namespace AElf.OS.Network.Events
{
    public class AnnouncementReceivedEventData
    {
        public PeerNewBlockAnnouncement Announce { get; private set; }
        public string Peer { get; private set; }
        
        public AnnouncementReceivedEventData(PeerNewBlockAnnouncement an, string peer)
        {
            Peer = peer;
            Announce = an;
        }
    }
}