using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote.Managers
{
    public interface IVoteResultManager
    {
        void AddVoteResult(Hash votingResultHash, Hash votingItemId,long snapshotNumber,long votersCount,long votAmount, Timestamp startTimestamp);

        void RemoveVoteResult();
       
        /// <summary>
        /// Update the State.VotingResults.include the VotersCount,VotesAmount and the votes int the results[option]
        /// </summary>
        /// <param name="votingItem"></param>
        /// <param name="option"></param>
        /// <param name="amount"></param>
        void UpdateVoteResult(Hash votingResultHash, string option, long amount);

        void WithdrawVoteResult(Hash votingResultHash, string option, long amount, bool isActive);

        VotingResult SaveVoteResult(Hash votingResultHash); 
        
        VotingResult GetVotingResult(Hash votingResultHash);
    }
}