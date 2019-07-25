using AElf.Types;

namespace AElf.OS.BlockSync.Dto
{
    public class SyncAnnouncementDto
    {
        public Hash SyncBlockHash { get; set; }

        public long SyncBlockHeight { get; set; }

        public string SuggestedPeerPubkey { get; set; }
        
        public int BatchRequestBlockCount { get; set; }
    }
}