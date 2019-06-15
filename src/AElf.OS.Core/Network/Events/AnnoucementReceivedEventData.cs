using System;

namespace AElf.OS.Network.Events
{
    public class AnnouncementReceivedEventData
    {
        public PeerNewBlockAnnouncement Announce { get; }
        public string SenderPubKey { get; }
        
        public DateTime CreateTime { get; set; }
        
        public AnnouncementReceivedEventData(PeerNewBlockAnnouncement an, string senderPubKey)
        {
            SenderPubKey = senderPubKey;
            Announce = an;
            CreateTime = DateTime.Now;
        }
    }
}