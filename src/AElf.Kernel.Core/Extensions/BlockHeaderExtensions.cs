using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel
{
    public static class BlockHeaderExtensions
    {
        public static bool IsMined(this BlockHeader blockHeader)
        {
            return blockHeader.MerkleTreeRootOfTransactions != null;
        }

        public static Hash GetPreMiningHash(this BlockHeader blockHeader)
        {
            return Hash.FromRawBytes(new BlockHeader()
            {
                PreviousBlockHash = blockHeader.PreviousBlockHash,
                Height = blockHeader.Height
            }.ToByteArray());
        }
    }
}