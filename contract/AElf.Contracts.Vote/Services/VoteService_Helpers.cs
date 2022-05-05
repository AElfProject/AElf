using AElf.Types;

namespace AElf.Contracts.Vote.Services
{
    internal partial class VoteService
    {
        private Hash GetVotingResultHash(Hash votingItemId, long snapshotNumber)
        {
            return new VotingResult
            {
                VotingItemId = votingItemId,
                SnapshotNumber = snapshotNumber
            }.GetHash();
        }
        
       
    }
}