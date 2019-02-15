using AElf.Common;

namespace AElf.Kernel
{
    public static class BlockHeaderExtensions
    {
        public static Hash GetDisambiguationHash(this BlockHeader blockHeader)
        {
            return HashHelpers.GetDisambiguationHash(blockHeader.Height, Hash.FromRawBytes(blockHeader.P.ToByteArray()));
        }
    }
}