using System;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner
{
    //TODO: should move to other project
    public class RequestMiningDto
    {
        public Hash PreviousBlockHash { get; set; }

        public long PreviousBlockHeight { get; set; }

        public Duration BlockExecutionTime { get; set; }
    }
}