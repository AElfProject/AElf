namespace AElf.Network.Connection
{
    public class PartialPacket
    {
        public int Position { get; set; }
        public bool IsEnd { get; set; }
        public int TotalDataSize { get; set; }
        
        public byte[] Data { get; set; }
    }
}