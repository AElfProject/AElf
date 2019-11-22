using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.OS.Network.Grpc
{
    internal class QueuedHash
    {
        public Hash ItemHash { get; set; }
        public Timestamp EnqueueTime { get; set; }
    }
}