using System;
using AElf.OS.Network.Application;
using AElf.Types;

namespace AElf.OS.Network.Grpc;

public class StreamJob
{
    public Transaction Transaction { get; set; }
    public BlockAnnouncement BlockAnnouncement { get; set; }
    public BlockWithTransactions BlockWithTransactions { get; set; }
    public LibAnnouncement LibAnnouncement { get; set; }
    public StreamMessage StreamMessage { get; set; }
    public Action<NetworkException> SendCallback { get; set; }
}