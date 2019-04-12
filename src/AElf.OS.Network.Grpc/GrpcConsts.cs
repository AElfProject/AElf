namespace AElf.OS.Network.Grpc
{
    public static class GrpcConsts
    {
        public const string PubkeyMetadataKey = "public-key";
        public const string PeerInfoMetadataKey = "peer-info";

        public const string GrpcRequestCompressKey = "grpc-internal-encoding-request";
        public const string GrpcGzipConst = "gzip";
    }
}