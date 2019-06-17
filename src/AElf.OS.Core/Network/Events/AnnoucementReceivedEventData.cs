using System;

namespace AElf.OS.Network.Events
{
    public class AnnouncementReceivedEvent
    {
        public PeerNewBlockAnnouncement Announce { get; }
        public string SenderPubKey { get; }
        
        public DateTime CreateTime { get; set; }
        
        public AnnouncementReceivedEvent(PeerNewBlockAnnouncement an, string senderPubKey)
        {
            SenderPubKey = senderPubKey;
            Announce = an;
            CreateTime = DateTime.Now;
        }
    }
}