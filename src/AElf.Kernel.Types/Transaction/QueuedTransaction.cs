using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel
{
    public enum RefBlockStatus
    {
        UnknownRefBlockStatus = 0,
        RefBlockValid = 1,
        RefBlockInvalid = -1,
        RefBlockExpired = -2
    }

    public class QueuedTransaction
    {
        public Transaction Transaction { get; set; }
        public Hash TransactionId { get; set; }
        public Timestamp EnqueueTime { get; set; }
        public RefBlockStatus RefBlockStatus { get; set; }
        public long BucketIndex { get; set; }
    }
}