using System;

namespace AElf.OS.Network.Grpc
{
    public class StreamJobStats
    {
        public TimeSpan QueueTime { get; set; }
        public TimeSpan SendBufferEnqueueTime { get; set; }
    }
}