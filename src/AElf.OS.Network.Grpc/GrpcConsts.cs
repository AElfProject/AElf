namespace AElf.OS.Network.Grpc
{
    public static class GrpcConsts
    {
        public const string PubkeyMetadataKey = "public-key";
        public const string PeerInfoMetadataKey = "peer-info";

        public const string GrpcRequestCompressKey = "grpc-internal-encoding-request";
        public const string GrpcGzipConst = "gzip";
        
        public const int DefaultMaxReceiveMessageLength = 100 * 1024 * 1024;
        public const int DefaultMaxSendMessageLength = 100 * 1024 * 1024;
    }
}