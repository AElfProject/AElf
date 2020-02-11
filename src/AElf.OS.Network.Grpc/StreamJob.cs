using System;
using System.Diagnostics;
using AElf.OS.Network.Application;
using AElf.Types;

namespace AElf.OS.Network.Grpc
{
    public class StreamJob
    {
        public Transaction Transaction { get; set; }
        public BlockAnnouncement BlockAnnouncement { get; set; }
        public BlockWithTransactions BlockWithTransactions { get; set; }
        public LibAnnouncement LibAnnouncement { get; set; }
        public Action<NetworkException, StreamJobStats> SendCallback { get; set; }
        public Stopwatch QueueStopwatch { get; set; }
    }
}