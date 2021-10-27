using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.BlockSync.Types
{
    public class BlockDownloadJobInfo
    {
        public string JobId { get; set; }
        
        public Hash TargetBlockHash { get; set; }

        public long TargetBlockHeight { get; set; }

        public string SuggestedPeerPubkey { get; set; }

        public int BatchRequestBlockCount { get; set; }

        public Hash CurrentTargetBlockHash { get; set; }

        public long CurrentTargetBlockHeight { get; set; }

        public Timestamp Deadline { get; set; }

        public bool IsFinished { get; set; }
    }
}