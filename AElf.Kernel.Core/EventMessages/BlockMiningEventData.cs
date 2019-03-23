using System;
using AElf.Common;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.EventMessages
{
    public class BlockMiningEventData
    {
        public Hash PreviousBlockHash { get; }
        public long PreviousBlockHeight { get; }
        public DateTime DueTime => DateTime.UtcNow.AddMilliseconds(TimeoutMilliseconds);
        public int TimeoutMilliseconds { get; }

        public BlockMiningEventData(Hash previousBlockHash, long previousBlockHeight,
            int timeoutMilliseconds)
        {
            PreviousBlockHash = previousBlockHash;
            PreviousBlockHeight = previousBlockHeight;
            TimeoutMilliseconds = timeoutMilliseconds;
            Console.WriteLine(timeoutMilliseconds);
            Console.WriteLine(DateTime.UtcNow.AddMilliseconds(TimeoutMilliseconds).ToTimestamp().ToString());
        }
    }
}