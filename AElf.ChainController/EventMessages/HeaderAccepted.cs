using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public class HeaderAccepted
    {
        public BlockHeader Header { get; private set; }
        
        public HeaderAccepted(BlockHeader header)
        {
            Header = header;
        }
    }
}