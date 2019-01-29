using AElf.Common;
using AElf.Cryptography.ECDSA;

namespace AElf.Kernel
{
    public static class BlockHeaderExtensions
    {
        public static ECSignature GetSignature(this BlockHeader blockHeader)
        {
            return new ECSignature(blockHeader.Sig.ToByteArray());
        }

        public static Hash GetDisambiguationHash(this BlockHeader blockHeader)
        {
            return HashHelpers.GetDisambiguationHash(blockHeader.Index, Hash.FromRawBytes(blockHeader.P.ToByteArray()));
        }
    }
}