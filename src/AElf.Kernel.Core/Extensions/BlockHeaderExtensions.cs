using System;
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

        public static Hash GetDisambiguatingHash(this BlockHeader blockHeader)
        {
            if(!blockHeader.IsMined())
                throw new InvalidOperationException("GetDisambiguatingHash: should only get mined block's DisambiguatingHash");

            return blockHeader.GetHash();
        }
    }
}