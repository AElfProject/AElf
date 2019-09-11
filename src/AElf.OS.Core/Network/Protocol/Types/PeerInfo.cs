using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.Network.Grpc
{
    public class PeerInfo
    {
        public string Pubkey { get; set; }
        public int ProtocolVersion { get; set; }
        public Timestamp ConnectionTime { get; set; }
        public bool IsInbound { get; set; }
    }
}