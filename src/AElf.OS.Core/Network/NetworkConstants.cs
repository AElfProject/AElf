namespace AElf.OS.Network;

public static class NetworkConstants
{
    public const int DefaultSslCertifFetchTimeout = 3000;
    public const int DefaultPeerDialTimeout = 3000;
    public const int DefaultPeerRecoveryTimeout = 3000;
    public const bool DefaultCompressBlocks = true;
    public const int DefaultRequestRetryCount = 1;
    public const int DefaultMaxPeers = 25;
    public const int DefaultMaxPeersPerIpAddress = 1;

    public const int DefaultSessionIdSize = 5;

    public const int DefaultMaxBlockAgeToBroadcastInMinutes = 10;

    public const int DefaultInitialSyncOffset = 512;

    public const int DefaultDiscoveryMaxNodesToRequest = 10;
    public const int DefaultDiscoveryPeriod = 60_000;
    public const int DefaultDiscoveryPeersToRequestCount = 5;
    public const int DefaultDiscoveryMaxNodesToKeep = 512;

    public const string PeerReconnectionQueueName = "PeerReconnectionQueue";
    public const string AnnouncementBroadcastQueueName = "AnnouncementBroadcastQueue";
    public const string TransactionBroadcastQueueName = "TransactionBroadcastQueue";
    public const string BlockBroadcastQueueName = "BlockBroadcastQueue";

    public const long HandshakeTimeout = 1500;

    public const long PeerConnectionTimeout = 10000;

    public const int DefaultMaxBufferedTransactionCount = 100;
    public const int DefaultMaxBufferedBlockCount = 50;
    public const int DefaultMaxBufferedAnnouncementCount = 200;

    public const int DefaultPeerReconnectionPeriod = 60_000; // 1 min
    public const int DefaultMaximumReconnectionTime = 60_000 * 60 * 24; // 1 day
    public const int DefaultPeerRemovalSeconds = 300;

    public const int DefaultPeerPort = 6800;

    public const int DefaultNtpDriftThreshold = 1_000;

    public const int DefaultPeerInvalidTransactionTimeout = 10_000;
    public const int DefaultPeerInvalidTransactionLimit = 50;
}