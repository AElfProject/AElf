using AElf.Types;

namespace AElf.OS.BlockSync.Types
{
    public class BlockDownloadJobInfo
    {
        public Hash TargetBlockHash { get; set; }

        public long TargetBlockHeight { get; set; }

        public string SuggestedPeerPubkey { get; set; }

        public int BatchRequestBlockCount { get; set; }
    }
}