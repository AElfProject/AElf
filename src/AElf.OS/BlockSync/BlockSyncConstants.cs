
namespace AElf.OS.BlockSync
{
    public class BlockSyncConstants
    {
        public const long BlockSyncFetchBlockAgeLimit = 1000;
        public const long BlockSyncAttachBlockAgeLimit = 2000;
        public const long BlockSyncAttachAndExecuteBlockAgeLimit = 4000;

        public const int BlockSyncModeHeightOffset = 12;
        public const int FetchBlockRetryTimes = 3;
        public const int SyncBlockRetryTimes = 3;
        public const int DefaultBlockDownloadTimerPeriod = 4000;
        public const int DefaultMaxBlockDownloadCount = 50;
        public const int DefaultMaxBatchRequestBlockCount = 10;
    }
}