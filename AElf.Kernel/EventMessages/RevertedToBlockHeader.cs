namespace AElf.Kernel.EventMessages
{
    public sealed class RevertedToBlockHeader
    {
        public RevertedToBlockHeader(BlockHeader header)
        {
            BlockHeader = header;
        }

        public BlockHeader BlockHeader { get; }
    }
}