namespace AElf.OS.Network.Events
{
    public class AnnouncementReceivedEventData
    {
        public bool IsFromConnection { get; }
        public PeerNewBlockAnnouncement Announce { get; }
        public string SenderPubKey { get; }
        
        public AnnouncementReceivedEventData(PeerNewBlockAnnouncement an, string senderPubKey, bool isFromConnection = false)
        {
            SenderPubKey = senderPubKey;
            Announce = an;
            IsFromConnection = isFromConnection;
        }
    }
}