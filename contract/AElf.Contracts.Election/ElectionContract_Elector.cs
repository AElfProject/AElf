using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election
{
    /// <summary>
    /// Vote & Withdraw
    /// </summary>
    public partial class ElectionContract
    {
        #region Vote

        /// <summary>
        /// Call the Vote function of VoteContract to do a voting.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Vote(VoteMinerInput input)
        {
            // Check candidate information map instead of candidates. 
            var targetInformation = State.CandidateInformationMap[input.CandidatePubkey];
            Assert(targetInformation != null, "Candidate not found.");
            if (targetInformation == null) return new Empty(); // Just to avoid IDE warning.
            Assert(targetInformation.IsCurrentCandidate, "Candidate quited election.");

            var lockSeconds = (input.EndTimestamp - Context.CurrentBlockTime).Seconds;
            Assert(lockSeconds >= State.MinimumLockTime.Value,
                $"Invalid lock time. At least {State.MinimumLockTime.Value.Div(60).Div(60).Div(24)} days");
            Assert(lockSeconds <= State.MaximumLockTime.Value,
                $"Invalid lock time. At most {State.MaximumLockTime.Value.Div(60).Div(60).Div(24)} days");

            State.LockTimeMap[Context.TransactionId] = input.EndTimestamp.Seconds.Sub(Context.CurrentBlockTime.Seconds);

            var recoveredPublicKey = Context.RecoverPublicKey();

            UpdateElectorInformation(recoveredPublicKey, input.Amount);

            UpdateCandidateInformation(input.CandidatePubkey, input.Amount);

            State.TokenContract.Lock.Send(new LockInput
            {
                Address = Context.Sender,
                Symbol = Context.Variables.NativeSymbol,
                LockId = Context.TransactionId,
                Amount = GetElfAmount(input.Amount),
                Usage = "Voting for Main Chain Miner Election."
            });

            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = ElectionContractConstants.VoteSymbol,
                To = Context.Sender,
                Amount = input.Amount,
                Memo = "Issue VOTEs."
            });

            State.VoteContract.Vote.Send(new VoteInput
            {
                Voter = Context.Sender,
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Amount = input.Amount,
                Option = input.CandidatePubkey,
                VoteId = Context.TransactionId
            });

            State.ProfitContract.AddWeight.Send(new AddWeightInput
            {
                ProfitId = State.WelfareHash.Value,
                Receiver = Context.Sender,
                Weight = GetVotesWeight(input.Amount, lockSeconds),
                EndPeriod = GetEndPeriod(lockSeconds) + 1
            });

            return new Empty();
        }

        /// <summary>
        /// Update Elector's Votes information.
        /// </summary>
        /// <param name="recoveredPublicKey"></param>
        /// <param name="amount"></param>
        private void UpdateElectorInformation(byte[] recoveredPublicKey, long amount)
        {
            var voterPublicKey = recoveredPublicKey.ToHex();
            var voterPublicKeyByteString = ByteString.CopyFrom(recoveredPublicKey);
            var voterVotes = State.ElectorVotes[voterPublicKey];
            if (voterVotes == null)
            {
                voterVotes = new ElectorVote
                {
                    Pubkey = voterPublicKeyByteString,
                    ActiveVotingRecordIds = {Context.TransactionId},
                    ActiveVotedVotesAmount = amount,
                    AllVotedVotesAmount = amount
                };
            }
            else
            {
                voterVotes.ActiveVotingRecordIds.Add(Context.TransactionId);
                voterVotes.ActiveVotedVotesAmount = voterVotes.ActiveVotedVotesAmount.Add(amount);
                voterVotes.AllVotedVotesAmount = voterVotes.AllVotedVotesAmount.Add(amount);
            }

            State.ElectorVotes[voterPublicKey] = voterVotes;
        }

        /// <summary>
        /// Update Candidate's Votes information.
        /// </summary>
        /// <param name="candidatePublicKey"></param>
        /// <param name="amount"></param>
        private void UpdateCandidateInformation(string candidatePublicKey, long amount)
        {
            var candidateVotes = State.CandidateVotes[candidatePublicKey];
            if (candidateVotes == null)
            {
                candidateVotes = new CandidateVote
                {
                    Pubkey = candidatePublicKey.ToByteString(),
                    ObtainedActiveVotingRecordIds = {Context.TransactionId},
                    ObtainedActiveVotedVotesAmount = amount,
                    AllObtainedVotedVotesAmount = amount
                };
            }
            else
            {
                candidateVotes.ObtainedActiveVotingRecordIds.Add(Context.TransactionId);
                candidateVotes.ObtainedActiveVotedVotesAmount =
                    candidateVotes.ObtainedActiveVotedVotesAmount.Add(amount);
                candidateVotes.AllObtainedVotedVotesAmount =
                    candidateVotes.AllObtainedVotedVotesAmount.Add(amount);
            }

            State.CandidateVotes[candidatePublicKey] = candidateVotes;
        }

        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            return lockTime.Div(86400).Div(270).Mul(votesAmount).Add(votesAmount.Mul(2).Div(3));
        }

        private long GetEndPeriod(long lockTime)
        {
            var treasury = State.ProfitContract.GetProfitItem.Call(State.TreasuryHash.Value);
            return lockTime.Div(State.TimeEachTerm.Value).Add(treasury.CurrentPeriod);
        }

        #endregion

        #region Withdraw

        /// <summary>
        /// Withdraw a voting,recall the votes the voted by sender.
        /// At the same time,the weight that the voter occupied will sub form totalWeight.
        /// and the "VOTE" token will be returned to ElectionContract;
        /// </summary>
        /// <param name="input">Hash</param>
        /// <returns></returns>
        public override Empty Withdraw(Hash input)
        {
            var votingRecord = State.VoteContract.GetVotingRecord.Call(input);

            var actualLockedTime = Context.CurrentBlockTime.Seconds.Sub(votingRecord.VoteTimestamp.Seconds);
            var claimedLockDays = State.LockTimeMap[input];
            Assert(actualLockedTime >= claimedLockDays,
                $"Still need {claimedLockDays.Sub(actualLockedTime).Div(86400)} days to unlock your token.");

            // Update Elector's Votes information.
            var voterPublicKey = Context.RecoverPublicKey().ToHex();
            var voterVotes = State.ElectorVotes[voterPublicKey];
            voterVotes.ActiveVotingRecordIds.Remove(input);
            voterVotes.WithdrawnVotingRecordIds.Add(input);
            voterVotes.ActiveVotedVotesAmount.Sub(votingRecord.Amount);
            State.ElectorVotes[voterPublicKey] = voterVotes;

            // Update Candidate's Votes information.
            var candidateVotes = State.CandidateVotes[votingRecord.Option];
            candidateVotes.ObtainedActiveVotingRecordIds.Remove(input);
            candidateVotes.ObtainedWithdrawnVotingRecordIds.Add(input);
            candidateVotes.ObtainedActiveVotedVotesAmount.Sub(votingRecord.Amount);
            State.CandidateVotes[votingRecord.Option] = candidateVotes;

            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Address = Context.Sender,
                Symbol = Context.Variables.NativeSymbol,
                Amount = GetElfAmount(votingRecord.Amount),
                LockId = input,
                Usage = "Withdraw votes for Main Chain Miner Election."
            });

            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = votingRecord.Amount,
                Symbol = ElectionContractConstants.VoteSymbol,
                Memo = "Return VOTE tokens."
            });

            State.VoteContract.Withdraw.Send(new WithdrawInput
            {
                VoteId = input
            });

            State.ProfitContract.SubWeight.Send(new SubWeightInput
            {
                ProfitId = State.WelfareHash.Value,
                Receiver = Context.Sender
            });

            return new Empty();
        }

        #endregion

        private long GetElfAmount(long votingAmount)
        {
            var elfAmount = votingAmount;
            for (var i = 0;
                i < ElectionContractConstants.ElfTokenDecimals.Sub(ElectionContractConstants.VoteTokenDecimals);
                i++)
            {
                elfAmount = elfAmount.Mul(10);
            }

            return elfAmount;
        }
    }
}