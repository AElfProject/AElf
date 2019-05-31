namespace AElf.OS.Network.Events
{
    public class PreLibAnnouncementReceivedEventData
    {
        public string SenderPubKey { get; }
        
        public PreLibAnnouncementReceivedEventData(string senderPubKey)
        {
            SenderPubKey = senderPubKey;
        }
    }
}