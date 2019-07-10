namespace AElf.OS.Network.Events
{
    public class PreLibAnnouncementReceivedEventData
    {
        public PreLibAnnouncement Announce { get; }
        public string SenderPubKey { get; }
        
        public PreLibAnnouncementReceivedEventData(PreLibAnnouncement an, string senderPubKey)
        {
            SenderPubKey = senderPubKey;
            Announce = an;
        }
    }
}