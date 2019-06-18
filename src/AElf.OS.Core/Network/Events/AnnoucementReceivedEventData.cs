using System;

namespace AElf.OS.Network.Events
{
    public class AnnouncementReceivedEvent
    {
        public PeerNewBlockAnnouncement Announce { get; }
        public string SenderPubKey { get; }
        
        public AnnouncementReceivedEvent(PeerNewBlockAnnouncement an, string senderPubKey)
        {
            SenderPubKey = senderPubKey;
            Announce = an;
        }
    }
}