using AElf.Common;
using AElf.Cryptography.ECDSA;

namespace AElf.Kernel
{
    public static class BlockHeaderExtensions
    {
        public static Hash GetDisambiguationHash(this BlockHeader blockHeader)
        {
            return HashHelpers.GetDisambiguationHash(blockHeader.Index, Hash.FromRawBytes(blockHeader.P.ToByteArray()));
        }
    }
}