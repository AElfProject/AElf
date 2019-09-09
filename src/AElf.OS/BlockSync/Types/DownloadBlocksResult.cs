using AElf.Types;

namespace AElf.OS.BlockSync.Types
{
    public class DownloadBlocksResult
    {
        public bool Success { get; set; }
        
        public int DownloadBlockCount { get; set; }

        public Hash LastDownloadBlockHash { get; set; }

        public long LastDownloadBlockHeight { get; set; }
    }
}