namespace AElf.OS.Network.Events
{
    public class AnnouncementReceivedEventData
    {
        public BlockAnnouncement Announce { get; }
        public string SenderPubKey { get; }
        
        public AnnouncementReceivedEventData(BlockAnnouncement an, string senderPubKey)
        {
            SenderPubKey = senderPubKey;
            Announce = an;
        }
    }
}