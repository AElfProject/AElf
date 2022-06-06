namespace AElf.OS.Network.Events;

public class AnnouncementReceivedEventData
{
    public AnnouncementReceivedEventData(BlockAnnouncement an, string senderPubKey)
    {
        SenderPubKey = senderPubKey;
        Announce = an;
    }

    public BlockAnnouncement Announce { get; }
    public string SenderPubKey { get; }
}