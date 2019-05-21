using AElf.Types;

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
            return new BlockHeader()
            {
                PreviousBlockHash = blockHeader.PreviousBlockHash,
                Height = blockHeader.Height
            }.GetHash();
        }
    }
}