
namespace AElf.OS.BlockSync
{
    public class BlockSyncConstants
    {
        public const long BlockSyncFetchBlockAgeLimit = 100000;
        public const long BlockSyncAttachBlockAgeLimit = 200000;
        public const long BlockSyncAttachAndExecuteBlockAgeLimit = 400000;

        public const int BlockSyncModeHeightOffset = 12;
        public const int DefaultBlockDownloadTimerPeriod = 1000;
        public const int DefaultMaxBlockDownloadCount = 50;
        public const int DefaultMaxBatchRequestBlockCount = 10;
    }
}