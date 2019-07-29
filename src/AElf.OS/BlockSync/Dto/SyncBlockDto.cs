using AElf.OS.Network;

namespace AElf.OS.BlockSync.Dto
{
    public class SyncBlockDto
    {
        public BlockWithTransactions BlockWithTransactions { get; set; }
        
        public string SuggestedPeerPubkey { get; set; }
        
        public int BatchRequestBlockCount { get; set; }
    }
}