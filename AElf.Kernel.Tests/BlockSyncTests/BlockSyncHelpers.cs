using System;
using AElf.Common.ByteArrayHelpers;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Tests.BlockSyncTests
{
    public static class BlockSyncHelpers
    {
        public static Block GenerateValidBlockToSync()
        {
            var block = new Block(ByteArrayHelpers.RandomFill(10));

            block.Header.ChainId = ByteArrayHelpers.RandomFill(10);
            block.Header.Time = Timestamp.FromDateTime(DateTime.UtcNow);
            block.Header.PreviousBlockHash = ByteArrayHelpers.RandomFill(256);
            
            return block;
        }
    }
}