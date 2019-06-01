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
    }
}