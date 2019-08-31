using System;
using AElf.OS.Network.Application;
using AElf.Types;

namespace AElf.OS.Network.Grpc
{
    public class StreamJob
    {
        public TransactionList TransactionList { get; set; }
        public BlockAnnouncement BlockAnnouncement { get; set; }
        public BlockWithTransactions BlockWithTransactions { get; set; }
        public Action<NetworkException> SendCallback { get; set; }
    }
}