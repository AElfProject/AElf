namespace AElf.OS.Network.Grpc
{
    public class GrpcPeerInfo
    {
        public string PublicKey { get; set; }
        public string PeerIpAddress { get; set; }
        public int ProtocolVersion { get; set; }
        public long ConnectionTime { get; set; }
        public long StartHeight { get; set; }
        public bool IsInbound { get; set; }
        public long LibHeightAtHandshake { get; set; }
    }
}