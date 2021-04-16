
namespace AElf.OS.BlockSync
{
    public class BlockSyncConstants
    {
        public const long BlockSyncFetchBlockAgeLimit = 30000;
        public const long BlockSyncAttachBlockAgeLimit = 10000;
        public const long BlockSyncAttachAndExecuteBlockAgeLimit = 30000;

        public const int BlockSyncModeHeightOffset = 12;
        public const int DefaultBlockDownloadTimerPeriod = 1000;
        public const int DefaultMaxBlockDownloadCount = 50;
        public const int DefaultMaxBatchRequestBlockCount = 10;
    }
}