using System;
using Google.Protobuf.WellKnownTypes;
using AElf.Common;

namespace AElf.Kernel.Tests.BlockSyncTests
{
    public static class BlockSyncHelpers
    {
        public static Block GenerateValidBlockToSync(ulong index = 0)
        {
//            var block = new Block(ByteArrayHelpers.RandomFill(10));
            var block = new Block(Hash.Generate());
//            block.Header.ChainId = ByteArrayHelpers.RandomFill(10);
            block.Header.ChainId = Hash.Generate();
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
//            block.Header.PreviousBlockHash = ByteArrayHelpers.RandomFill(256);
            block.Header.PreviousBlockHash = Hash.Generate();
            block.Header.MerkleTreeRootOfWorldState = Hash.Default;
            block.FillTxsMerkleTreeRootInHeader();
            block.Header.Index = index;
            block.Body.BlockHeader = block.Header.GetHash();
            return block;
        }
    }
}