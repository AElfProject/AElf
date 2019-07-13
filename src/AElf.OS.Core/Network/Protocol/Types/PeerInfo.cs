namespace AElf.OS.Network.Grpc
{
    public class PeerInfo
    {
        public string Pubkey { get; set; }
        public int ProtocolVersion { get; set; }
        public long ConnectionTime { get; set; }
        public bool IsInbound { get; set; }
    }
}