using AElf.Types;

namespace AElf.OS.BlockSync.Events
{
    public class BadPeerFoundEventData
    {
        public Hash BlockHash { get; set;}
        public long BlockHeight { get; set;}
        public string PeerPubkey { get; set; }
    }
}