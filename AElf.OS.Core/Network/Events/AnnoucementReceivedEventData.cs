using AElf.Kernel;

namespace AElf.OS.Network.Events
{
    public class AnnoucementReceivedEventData
    {
        public BlockHeader Header { get; private set; }
        public string Peer { get; private set; }
        
        public AnnoucementReceivedEventData(BlockHeader header, string peer)
        {
            Peer = peer;
            Header = header;
        }
    }
}