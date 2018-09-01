// ReSharper disable once CheckNamespace

using System.Collections.Generic;

namespace AElf.Kernel
{
    public class BackupChain
    {
        public ulong StartHeight { get; set; }
        public ulong EndHeight { get; set; }
        public List<IBlock> Blocks { get; set; }
        public Hash FirstBlockHash { get; set; }
        public Hash ForkHash { get; set; }
    }
}