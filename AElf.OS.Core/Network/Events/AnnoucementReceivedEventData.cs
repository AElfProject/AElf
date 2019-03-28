namespace AElf.OS.Network.Events
{
    public class AnnouncementReceivedEventData
    {
        public AnnouncementReceivedEventData(PeerNewBlockAnnouncement an, string senderPubKey)
        {
            SenderPubKey = senderPubKey;
            Announce = an;
        }

        public PeerNewBlockAnnouncement Announce { get; }
        public string SenderPubKey { get; }
    }
}