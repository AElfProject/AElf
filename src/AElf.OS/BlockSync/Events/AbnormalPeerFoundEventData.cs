using AElf.Types;

namespace AElf.OS.BlockSync.Events
{
    public class AbnormalPeerFoundEventData
    {
        public Hash BlockHash { get; set;}
        public long BlockHeight { get; set;}
        public string PeerPubkey { get; set; }
    }
}