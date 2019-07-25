using AElf.Types;

namespace AElf.OS.BlockSync.Dto
{
    public class DownloadBlockDto
    {
        public Hash PreviousBlockHash { get; set; }

        public long PreviousBlockHeight { get; set; }

        public int BatchRequestBlockCount { get; set; }

        public int MaxBlockDownloadCount { get; set; }

        public string SuggestedPeerPubkey { get; set; }
    }
}