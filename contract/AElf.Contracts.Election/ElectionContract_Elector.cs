using System.Linq;
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

            State.LockTimeMap[Context.TransactionId] = lockSeconds;

            var recoveredPublicKey = Context.RecoverPublicKey();

            UpdateElectorInformation(recoveredPublicKey, input.Amount);

            var candidateVotesAmount = UpdateCandidateInformation(input.CandidatePubkey, input.Amount);

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

            State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
            {
                SchemeId = State.WelfareHash.Value,
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = Context.Sender,
                    Shares = GetVotesWeight(input.Amount, lockSeconds)
                },
                EndPeriod = GetEndPeriod(lockSeconds) + 1
            });

            var rankingList = State.ValidationDataCentersRankingList.Value;
            if (State.ValidationDataCentersRankingList.Value.ValidationDataCenters.ContainsKey(input.CandidatePubkey))
            {
                rankingList.ValidationDataCenters[input.CandidatePubkey] =
                    rankingList.ValidationDataCenters[input.CandidatePubkey].Add(input.Amount);
                State.ValidationDataCentersRankingList.Value = rankingList;
            }

            if (State.Candidates.Value.Value.Count > GetValidationDataCenterCount() &&
                !State.ValidationDataCentersRankingList.Value.ValidationDataCenters.ContainsKey(input.CandidatePubkey))
            {
                TryToBecomeAValidationDataCenter(input, candidateVotesAmount, rankingList);
            }

            return new Empty();
        }

        private void TryToBecomeAValidationDataCenter(VoteMinerInput input, long candidateVotesAmount,
            ValidationDataCenterRankingList rankingList)
        {
            var minimumVotes = candidateVotesAmount;
            var minimumVotesCandidate = input.CandidatePubkey;
            bool replaceWillHappen = false;
            foreach (var pubkeyToVotesAmount in rankingList.ValidationDataCenters.Reverse())
            {
                if (pubkeyToVotesAmount.Value < minimumVotes)
                {
                    replaceWillHappen = true;
                    minimumVotesCandidate = pubkeyToVotesAmount.Key;
                    break;
                }
            }

            if (replaceWillHappen)
            {
                State.ValidationDataCentersRankingList.Value.ValidationDataCenters.Remove(minimumVotesCandidate);
                State.ValidationDataCentersRankingList.Value.ValidationDataCenters.Add(input.CandidatePubkey,
                    candidateVotesAmount);
                State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
                {
                    SchemeId = State.SubsidyHash.Value,
                    Beneficiary = Address.FromPublicKey(ByteArrayHelper.FromHexString(minimumVotesCandidate))
                });
                State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
                {
                    SchemeId = State.SubsidyHash.Value,
                    BeneficiaryShare = new BeneficiaryShare
                    {
                        Beneficiary = Address.FromPublicKey(ByteArrayHelper.FromHexString(input.CandidatePubkey)),
                        Shares = 1
                    }
                });
            }
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
        private long UpdateCandidateInformation(string candidatePublicKey, long amount)
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

            return candidateVotes.ObtainedActiveVotedVotesAmount;
        }

        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            return lockTime.Mul(1_0000).Div(86400).Div(270).Mul(votesAmount).Add(votesAmount.Mul(2).Div(3));
        }

        private long GetEndPeriod(long lockTime)
        {
            var treasury = State.ProfitContract.GetScheme.Call(State.TreasuryHash.Value);
            return lockTime.Div(State.TimeEachTerm.Value).Add(treasury.CurrentPeriod);
        }

        #endregion

        #region Withdraw

        /// <summary>
        /// Withdraw a voting,recall the votes the voted by sender.
        /// At the same time,the Shares that the voter occupied will sub form TotalShares.
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

            State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
            {
                SchemeId = State.WelfareHash.Value,
                Beneficiary = Context.Sender
            });

            var rankingList = State.ValidationDataCentersRankingList.Value;
            if (State.ValidationDataCentersRankingList.Value.ValidationDataCenters.ContainsKey(votingRecord.Option))
            {
                rankingList.ValidationDataCenters[votingRecord.Option] =
                    rankingList.ValidationDataCenters[votingRecord.Option].Sub(votingRecord.Amount);
                State.ValidationDataCentersRankingList.Value = rankingList;
            }


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