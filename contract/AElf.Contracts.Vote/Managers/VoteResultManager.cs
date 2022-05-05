using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Sdk.CSharp.State;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote.Managers
{
    public class VoteResultManager:IVoteResultManager
    {
        private readonly CSharpSmartContractContext _context;
        private readonly MappedState<Hash, VotingResult> _votingResultMap;

        public VoteResultManager(CSharpSmartContractContext context, MappedState<Hash, VotingResult> votingResultMap)
        {
            _context = context;
            _votingResultMap = votingResultMap;
        }
        public void AddVoteResult(Hash votingResultHash, Hash votingItemId, long snapshotNumber, long votersCount, long votAmount, Timestamp startTimestamp)
        {
            var votingResult = new VotingResult
            {
                VotingItemId = votingItemId,
                SnapshotNumber = snapshotNumber,
                VotersCount = votersCount,
                VotesAmount = votAmount,
                SnapshotStartTimestamp = startTimestamp
            };
            _votingResultMap[votingResultHash] = votingResult;
        }

        public void RemoveVoteResult()
        {
            throw new System.NotImplementedException();
        }

        public void UpdateVoteResult(Hash votingResultHash, string option, long amount)
        {
            var votingResult = _votingResultMap[votingResultHash];
            if (!votingResult.Results.ContainsKey(option))
            {
                votingResult.Results.Add(option, 0);
            }

            var currentVotes = votingResult.Results[option];
            votingResult.Results[option] = currentVotes.Add(amount);
            votingResult.VotersCount = votingResult.VotersCount.Add(1);
            votingResult.VotesAmount = votingResult.VotesAmount.Add(amount);
            _votingResultMap[votingResultHash] = votingResult;
        }

        public void WithdrawVoteResult(Hash votingResultHash, string option, long amount, bool isActive)
        {
            var votingResult = _votingResultMap[votingResultHash];
            if (!votingResult.Results.ContainsKey(option))
            {
                throw new AssertionException("Voting option not found.");
            }
            var currentVotes = votingResult.Results[option];
            votingResult.Results[option] = currentVotes.Sub(amount);
            if (!isActive)
            {
                votingResult.VotersCount = votingResult.VotersCount.Sub(1);
            }
           
            votingResult.VotesAmount = votingResult.VotesAmount.Sub(amount);
            _votingResultMap[votingResultHash] = votingResult;
        }

        public VotingResult SaveVoteResult(Hash votingResultHash)
        {
            var previousVotingResult =_votingResultMap[votingResultHash];
            previousVotingResult.SnapshotEndTimestamp = _context.CurrentBlockTime;
            _votingResultMap[votingResultHash] = previousVotingResult;
            return previousVotingResult;
        }

        public VotingResult GetVotingResult(Hash votingResultHash)
        {
            return _votingResultMap[votingResultHash];
        }
    }
}