namespace AElf.OS.Network.Protocol.Types
{
    public class PeerConnectionInfo
    {
        public string Pubkey { get; set; }
        public int ProtocolVersion { get; set; }
        public long ConnectionTime { get; set; }
        public bool IsInbound { get; set; }
    }
}