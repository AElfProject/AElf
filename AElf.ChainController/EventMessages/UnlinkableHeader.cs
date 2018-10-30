using AElf.Kernel;

namespace AElf.ChainController.EventMessages
{
    public class UnlinkableHeader
    {
        public BlockHeader Header { get; private set; }

        public UnlinkableHeader(BlockHeader header)
        {
            Header = header;
        }
    }
}