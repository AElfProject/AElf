namespace AElf.OS.Network.Events
{
    public class AnnouncementReceivedEventData
    {
        public PeerNewBlockAnnouncement Announce { get; }
        public string SenderPubKey { get; }
        
        public AnnouncementReceivedEventData(PeerNewBlockAnnouncement an, string senderPubKey)
        {
            SenderPubKey = senderPubKey;
            Announce = an;
        }
    }
}