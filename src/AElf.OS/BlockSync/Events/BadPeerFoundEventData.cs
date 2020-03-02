using AElf.Types;

namespace AElf.OS.BlockSync.Events
{
    //TODO: rename BadPeer, who is bad man?
    public class BadPeerFoundEventData
    {
        public Hash BlockHash { get; set;}
        public long BlockHeight { get; set;}
        public string PeerPubkey { get; set; }
    }
}