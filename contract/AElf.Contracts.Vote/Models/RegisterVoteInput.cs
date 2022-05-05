using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote.Models
{
    public class RegisterVoteInput
    {
        public Hash VotingItemId { get; set; }
        public string AcceptedCurrency { get; set; }
        public bool IsLockToken { get; set; }
        public long CurrentSnapShotNumber { get; set; }
        public long TotalSnapshotNumber { get; set; }
        public Timestamp StartTimestamp { get; set; }
        public Timestamp EndTimestamp { get; set; }
        public Timestamp CurrentSnapshotStartTimestamp { get; set; }
        public RepeatedField<string> Options { get; set; }
    }
}