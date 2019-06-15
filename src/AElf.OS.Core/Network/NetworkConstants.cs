namespace AElf.OS.Network
{
    public static class NetworkConstants
    {
        public const int DefaultPeerDialTimeoutInMilliSeconds = 3000;
        public const int DefaultBlockRequestCount = 10;
        public const bool DefaultCompressBlocks = true;
        public const int DefaultMaxRequestRetryCount = 1;
        public const int DefaultMaxRandomPeersPerRequest = 2;
        public const int DefaultMinBlockGapBeforeSync = 1024;
        public const int DefaultMaxPeers = 25;
        
        public const int DefaultAnnouncementQueueWorkerCount = 20;
        public const int DefaultTransactionQueueWorkerCount = 4;
        
        public const int DefaultQueueWorkerCount = 10;

        public const string AnnouncementQueueName = "AnnouncementQueue";
        public const string TransactionQueueName = "TransactionQueue";
        
        public const string ReceivedAnnouncementsQueueName = "ReceivedAnnouncements";
        public const string ReceivedTransactionsQueueName = "ReceivedTransactions";
        
        public const int AnnouncementQueueJobTimeout = 500;
        public const int TransactionQueueJobTimeout = 500;
    }
}