using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Vote.Managers;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Vote.Services
{
    internal partial class VoteService:IVoteService
    {
        private readonly CSharpSmartContractContext _context;
        private readonly TokenContractContainer.TokenContractReferenceState _tokenContract;
        private readonly IVoteItemManager _voteItemManager;
        private readonly IVoteRecordManager _voteRecordManager;
        private readonly IVoteResultManager _voteResultManager;
        private readonly IVotedItemManager _votedItemManager;

        public VoteService(CSharpSmartContractContext context,
            TokenContractContainer.TokenContractReferenceState tokenContract,
            IVoteItemManager voteItemManager,
            IVoteRecordManager voteRecordManager,
            IVoteResultManager voteResultManager,
            IVotedItemManager votedItemManager)
        {
            _context = context;
            _tokenContract = tokenContract;
            _voteItemManager = voteItemManager;
            _voteRecordManager = voteRecordManager;
            _voteResultManager = voteResultManager;
            _votedItemManager = votedItemManager;
        }

        public void RegisterVote(Hash votingItemId, string acceptedCurrency, bool isLockToken,
            long totalSnapshotNumber, Timestamp startTimestamp, Timestamp endTimestamp, RepeatedField<string> options)
        {
            var isInWhiteList = _tokenContract.IsInWhiteList.Call(new IsInWhiteListInput
            {
                Symbol = acceptedCurrency,
                Address = _context.Self
            }).Value;
            if (!isInWhiteList)
            {
                throw new AssertionException("Claimed accepted token is not available for voting.");
            }
            _voteItemManager.AddVoteItem(votingItemId, acceptedCurrency, isLockToken,
                 totalSnapshotNumber, startTimestamp, endTimestamp, options);
            var votingResultHash = GetVotingResultHash(votingItemId, 1);
            _voteResultManager.AddVoteResult(votingResultHash, votingItemId,1,0,0, startTimestamp);
            _context.Fire(new VotingItemRegistered
            {
                Sponsor = _context.Sender,
                VotingItemId = votingItemId,
                AcceptedCurrency = acceptedCurrency,
                IsLockToken = isLockToken,
                TotalSnapshotNumber = totalSnapshotNumber,
                CurrentSnapshotNumber = 1,
                CurrentSnapshotStartTimestamp = startTimestamp,
                StartTimestamp = startTimestamp,
                EndTimestamp = endTimestamp,
                RegisterTimestamp = _context.CurrentBlockTime
            });
        }
        

        public void Vote(Hash voteId, Hash votingItemId, long amount, string option, Address voter,
            bool isChangerTarget)
        {
            var votingItem = _voteItemManager.GetVotingItem(votingItemId);
            if (option.Length > VoteContractConstants.OptionLengthLimit)
            {
                throw new AssertionException("Invalid input.");
            }

            if (!votingItem.Options.Contains(option))
            {
                throw new AssertionException( $"Option {option} not found.");
            }

            if (votingItem.CurrentSnapshotNumber > votingItem.TotalSnapshotNumber)
            {
                throw new AssertionException( "Current voting item already ended.");
            }
            var votingResultHash = GetVotingResultHash(votingItem.VotingItemId, votingItem.CurrentSnapshotNumber);
            if (!votingItem.IsLockToken)
            {
                if (votingItem.Sponsor != _context.Sender)
                {
                    throw new AssertionException( "Sender of delegated voting event must be the Sponsor.");
                }

                if (voter == null)
                {
                    throw new AssertionException( "Voter cannot be null if voting event is delegated.");
                }

                if (voteId == null)
                {
                    throw new AssertionException( "Vote Id cannot be null if voting event is delegated.");
                }
            }
            else
            {
                var votingResult = _voteResultManager.GetVotingResult(votingResultHash);
                // Voter = Transaction Sender
                voter = _context.Sender;
                // VoteId = Transaction Id;
                voteId = _context.GenerateId(_context.Self, votingResult.VotesAmount.ToBytes(false));
            }
            var votingRecord = _voteRecordManager.AddVoteRecord(voteId, votingItemId, amount, votingItem.CurrentSnapshotNumber, option, voter, isChangerTarget);
            _voteResultManager.UpdateVoteResult(votingResultHash, option, amount);
            _votedItemManager.UpdateVotedItems(voteId, votingItemId, voter);
            if (votingItem.IsLockToken)
            {
                // Lock voted token.
                _tokenContract.Lock.Send(new LockInput
                {
                    Address = votingRecord.Voter,
                    Symbol = votingItem.AcceptedCurrency,
                    LockId = voteId,
                    Amount = amount
                });
            }

            _context.Fire(new Voted
            {
                VoteId = voteId,
                VotingItemId = votingRecord.VotingItemId,
                Voter = votingRecord.Voter,
                Amount = votingRecord.Amount,
                Option = votingRecord.Option,
                SnapshotNumber = votingRecord.SnapshotNumber,
                VoteTimestamp = votingRecord.VoteTimestamp
            });
        }

        public void Withdraw(Hash voteId)
        {
            var votingRecord = _voteRecordManager.GetVoteRecord(voteId);

            var votingItem = _voteItemManager.GetVotingItem(votingRecord.VotingItemId);

            if (votingItem.IsLockToken)
            {
                if (votingRecord.Voter!=_context.Sender)
                {
                    throw new AssertionException( "No permission to withdraw votes of others."); 
                }
            }
            else
            {
                if (votingItem.Sponsor != _context.Sender)
                {
                    throw new AssertionException( "No permission to withdraw votes of others.");
                }
            }

            // Update VotingRecord.
            votingRecord.IsWithdrawn = true;
            votingRecord.WithdrawTimestamp = _context.CurrentBlockTime;
            _voteRecordManager.UpdateVoteRecord(voteId, votingRecord);

            var votingResultHash = GetVotingResultHash(votingRecord.VotingItemId, votingRecord.SnapshotNumber);
            
            var votedItems = _votedItemManager.WithdrawVotedItems(voteId, votingItem.VotingItemId, votingRecord.Voter);

            var isActive = votedItems.VotedItemVoteIds[votingRecord.VotingItemId.ToHex()].ActiveVotes.Any();
            _voteResultManager.WithdrawVoteResult(votingResultHash, votingRecord.Option, votingRecord.Amount, isActive);
            
            if (votingItem.IsLockToken)
            {
                _tokenContract.Unlock.Send(new UnlockInput
                {
                    Address = votingRecord.Voter,
                    Symbol = votingItem.AcceptedCurrency,
                    Amount = votingRecord.Amount,
                    LockId = voteId
                });
            }

            _context.Fire(new Withdrawn
            {
                VoteId = voteId,
                Option = votingRecord.Option,
                Voter = votingRecord.Voter,
                Amount = votingRecord.Amount,
                VotingItemId = votingRecord.VotingItemId
            });
        }

        public void TakeSnapshot(Hash votingItemId, long snapshotNumber)
        {
            var votingItem = _voteItemManager.GetVotingItem(votingItemId);
            if (votingItem.Sponsor != _context.Sender)
            {
                throw new AssertionException("Only sponsor can take snapshot.");
            }

            if (votingItem.CurrentSnapshotNumber - 1 >= votingItem.TotalSnapshotNumber)
            {
                throw new AssertionException("Current voting item already ended.");
            }

            var previousVotingResultHash = GetVotingResultHash(votingItemId, votingItem.CurrentSnapshotNumber);
            var previousVotingResult = _voteResultManager.SaveVoteResult(previousVotingResultHash);
            if (votingItem.CurrentSnapshotNumber != snapshotNumber)
            {
                throw new AssertionException(
                    $"Can only take snapshot of current snapshot number: {votingItem.CurrentSnapshotNumber}, but {snapshotNumber}");
            }

            var nextSnapshotNumber = _voteItemManager.UpdateSnapshotNumber(votingItemId);
            
            var currentVotingGoingHash = GetVotingResultHash(votingItemId, nextSnapshotNumber);
            _voteResultManager.AddVoteResult(currentVotingGoingHash, votingItemId, nextSnapshotNumber, previousVotingResult.VotersCount,previousVotingResult.VotesAmount,_context.CurrentBlockTime);
        }



        public void Claim()
        {
            throw new System.NotImplementedException();
        }
    }
}