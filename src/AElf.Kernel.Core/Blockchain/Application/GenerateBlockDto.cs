using System;
using AElf.Types;

namespace AElf.Kernel.Blockchain.Application
{
    public class GenerateBlockDto
    {
        public Hash PreviousBlockHash { get; set; }
        public long PreviousBlockHeight { get; set; }

        public DateTime BlockTime { get; set; } = DateTime.UtcNow;
    }
}