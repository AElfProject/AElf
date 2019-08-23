using System;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner
{
    public class RequestMiningDto
    {
        public Hash PreviousBlockHash { get; set; }

        public long PreviousBlockHeight { get; set; }

        public Duration BlockExecutionTime { get; set; }
    }
}