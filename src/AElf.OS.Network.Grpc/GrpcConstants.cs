namespace AElf.OS.Network.Grpc;

public static class GrpcConstants
{
    public const string PubkeyMetadataKey = "public-key";
    public const string SessionIdMetadataKey = "session-id-bin";
    public const string PeerInfoMetadataKey = "peer-info";
    public const string TimeoutMetadataKey = "timeout";
    public const string RetryCountMetadataKey = "retry-count";
    public const string GrpcRequestCompressKey = "grpc-internal-encoding-request";

    public const string GrpcArgKeepalivePermitWithoutCalls = "grpc.keepalive_permit_without_calls";
    public const string GrpcArgHttp2MaxPingsWithoutData = "grpc.http2_max_pings_without_data";
    public const string GrpcArgKeepaliveTimeoutMs = "grpc.keepalive_timeout_ms";
    public const string GrpcArgKeepaliveTimeMs = "grpc.keepalive_time_ms";

    public const int GrpcArgKeepalivePermitWithoutCallsOpen = 1;
    public const int GrpcArgHttp2MaxPingsWithoutDataVal = 0;
    public const int GrpcArgKeepaliveTimeoutMsVal = 60 * 1000;
    public const int GrpcArgKeepaliveTimeMsVal = 2 * 60 * 60 * 1000;

    public const string GrpcGzipConst = "gzip";

    public const int DefaultRequestTimeout = 200;

    public const int DefaultMaxReceiveMessageLength = 100 * 1024 * 1024;
    public const int DefaultMaxSendMessageLength = 100 * 1024 * 1024;

    public const int MaxSendBlockCountLimit = 50;

    public const string DefaultTlsCommonName = "aelf";

    public const int DefaultDiscoveryMaxNodesToResponse = 10;
}