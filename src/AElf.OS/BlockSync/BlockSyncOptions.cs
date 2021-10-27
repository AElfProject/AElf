namespace AElf.OS.BlockSync
{
    public class BlockSyncOptions
    {
        /// <summary>
        /// The timer period of download blocks (milliseconds).
        /// </summary>
        public int BlockDownloadTimerPeriod { get; set; } = BlockSyncConstants.DefaultBlockDownloadTimerPeriod;
        
        /// <summary>
        /// The maximum number of download blocks per download task.
        /// </summary>
        public int MaxBlockDownloadCount { get; set; } = BlockSyncConstants.DefaultMaxBlockDownloadCount;

        /// <summary>
        /// The maximum number of blocks per request.
        /// </summary>
        public int MaxBatchRequestBlockCount { get; set; } = BlockSyncConstants.DefaultMaxBatchRequestBlockCount;
    }
}