namespace AElf.OS.Handlers
{
    public class PeerConnectedEventData
    {
        public byte[] BlockId { get; set; }
        public string Peer { get; set; }
    }
}