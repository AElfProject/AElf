using System;
using AElf.Types;

namespace AElf.Kernel.Miner
{
    public class RequestMiningDto
    {
        public Hash PreviousBlockHash { get; set; }

        public long PreviousBlockHeight { get; set; }

        public TimeSpan BlockExecutionTime { get; set; }
    }
}