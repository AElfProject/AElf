using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
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
            AssertValidCandidateInformation(targetInformation);

            var lockSeconds = (input.EndTimestamp - Context.CurrentBlockTime).Seconds;
            AssertValidLockSeconds(lockSeconds);

            State.LockTimeMap[Context.TransactionId] = lockSeconds;

            var recoveredPublicKey = Context.RecoverPublicKey();

            UpdateElectorInformation(recoveredPublicKey, input.Amount);

            var candidateVotesAmount = UpdateCandidateInformation(input.CandidatePubkey, input.Amount);

            CallTokenContractLock(input.Amount);

            CallTokenContractIssue(input.Amount);

            CallVoteContractVote(input.Amount, input.CandidatePubkey);

            var votesWeight = GetVotesWeight(input.Amount, lockSeconds);

            CallProfitContractAddBeneficiary(votesWeight, lockSeconds);

            var rankingList = State.DataCentersRankingList.Value;
            if (State.DataCentersRankingList.Value.DataCenters.ContainsKey(input.CandidatePubkey))
            {
                rankingList.DataCenters[input.CandidatePubkey] =
                    rankingList.DataCenters[input.CandidatePubkey].Add(input.Amount);
                State.DataCentersRankingList.Value = rankingList;
            }

            if (State.Candidates.Value.Value.Count > GetValidationDataCenterCount() &&
                !State.DataCentersRankingList.Value.DataCenters.ContainsKey(input.CandidatePubkey))
            {
                TryToBecomeAValidationDataCenter(input, candidateVotesAmount, rankingList);
            }

            return new Empty();
        }

        private void TryToBecomeAValidationDataCenter(VoteMinerInput input, long candidateVotesAmount,
            DataCenterRankingList rankingList)
        {
            var minimumVotes = candidateVotesAmount;
            var minimumVotesCandidate = input.CandidatePubkey;
            bool replaceWillHappen = false;
            foreach (var pubkeyToVotesAmount in rankingList.DataCenters.Reverse())
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
                State.DataCentersRankingList.Value.DataCenters.Remove(minimumVotesCandidate);
                State.DataCentersRankingList.Value.DataCenters.Add(input.CandidatePubkey,
                    candidateVotesAmount);
                State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
                {
                    SchemeId = State.SubsidyHash.Value,
                    Beneficiary = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(minimumVotesCandidate))
                });
                State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
                {
                    SchemeId = State.SubsidyHash.Value,
                    BeneficiaryShare = new BeneficiaryShare
                    {
                        Beneficiary =
                            Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(input.CandidatePubkey)),
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
        private const int DaySec = 86400;

        private readonly Dictionary<int, decimal> _interestMap = new Dictionary<int, decimal>
        {
            {1 * 365 * DaySec, 1.001m},           // compound interest
            {2 * 365 * DaySec, 1.0015m},
            {3 * 365 * DaySec, 1.002m}
        };

        private const decimal DefaultInterest = 1.0022m;   // if locktime > 3 years, use this interest
        private const int Scale = 10000;   
        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            long calculated = 1;
            foreach (var instMap in _interestMap)  // calculate with different interest according to locktime
            {
                if(lockTime > instMap.Key)
                    continue;
                calculated = calculated.Mul((long)(Pow(instMap.Value, (uint)lockTime.Div(DaySec)) * Scale));
                break;
            }
            if (calculated == 1)   // locktime > 3 years
                calculated = calculated.Mul((long) (Pow(DefaultInterest, (uint)lockTime.Div(DaySec)) * Scale));
            return votesAmount.Mul(calculated).Add(votesAmount.Div(2));  // weight = locktime + voteAmount 
        }
        
        private decimal Pow(decimal x, uint y)
        {
            if (y == 1)
                return (long)x;
            decimal a = 1m;
            if (y == 0)
                return a;
            BitArray e = new BitArray(BitConverter.GetBytes(y));
            int t = e.Count;
            for (int i = t - 1; i >= 0; --i)
            {
                a *= a;
                if (e[i] == true)
                {
                    a *= x;
                }
            }
            return a;
        }
        
        private long GetEndPeriod(long lockTime)
        {
            var treasury = State.ProfitContract.GetScheme.Call(State.TreasuryHash.Value);
            return lockTime.Div(State.TimeEachTerm.Value).Add(treasury.CurrentPeriod);
        }

        #endregion

        #region ChangeVotingTarget

        public override Empty ChangeVotingOption(ChangeVotingOptionInput input)
        {
            var votingRecord = State.VoteContract.GetVotingRecord.Call(input.VoteId);
            var actualLockedTime = Context.CurrentBlockTime.Seconds.Sub(votingRecord.VoteTimestamp.Seconds);
            var claimedLockDays = State.LockTimeMap[input.VoteId];
            Assert(actualLockedTime < claimedLockDays, "This vote already expired.");

            // Withdraw old votes
            State.VoteContract.Withdraw.Send(new WithdrawInput
            {
                VoteId = input.VoteId
            });

            // Create new votes
            State.VoteContract.Vote.Send(new VoteInput
            {
                VoteId = input.VoteId,
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Amount = votingRecord.Amount,
                Voter = votingRecord.Voter,
                Option = input.CandidatePubkey,
                IsChangeTarget = true
            });

            // Update related candidate
            var oldCandidateVotes = State.CandidateVotes[votingRecord.Option];
            oldCandidateVotes.ObtainedActiveVotingRecordIds.Remove(input.VoteId);
            oldCandidateVotes.ObtainedActiveVotedVotesAmount =
                oldCandidateVotes.ObtainedActiveVotedVotesAmount.Sub(votingRecord.Amount);
            State.CandidateVotes[votingRecord.Option] = oldCandidateVotes;

            var newCandidateVotes = State.CandidateVotes[input.CandidatePubkey];
            if( newCandidateVotes != null )
            {
                newCandidateVotes.ObtainedActiveVotingRecordIds.Add(input.VoteId);
                newCandidateVotes.ObtainedActiveVotedVotesAmount =
                    newCandidateVotes.ObtainedActiveVotedVotesAmount.Add(votingRecord.Amount);
                State.CandidateVotes[input.CandidatePubkey] = newCandidateVotes;
            }
            else
            {
                State.CandidateVotes[input.CandidatePubkey] = new CandidateVote
                {
                    Pubkey = input.CandidatePubkey.ToByteString(),
                    ObtainedActiveVotingRecordIds = {input.VoteId},
                    ObtainedActiveVotedVotesAmount = votingRecord.Amount,
                    AllObtainedVotedVotesAmount = votingRecord.Amount
                };
            }
            return new Empty();
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
            voterVotes.ActiveVotedVotesAmount = voterVotes.ActiveVotedVotesAmount.Sub(votingRecord.Amount);
            State.ElectorVotes[voterPublicKey] = voterVotes;

            // Update Candidate's Votes information.
            var candidateVotes = State.CandidateVotes[votingRecord.Option];
            candidateVotes.ObtainedActiveVotingRecordIds.Remove(input);
            candidateVotes.ObtainedWithdrawnVotingRecordIds.Add(input);
            candidateVotes.ObtainedActiveVotedVotesAmount = candidateVotes.ObtainedActiveVotedVotesAmount.Sub(votingRecord.Amount);
            State.CandidateVotes[votingRecord.Option] = candidateVotes;

            CallTokenContractUnlock(input, votingRecord.Amount);
            CallTokenContractTransferFrom(votingRecord.Amount);
            CallVoteContractWithdraw(input);
            CallProfitContractRemoveBeneficiary();

            var rankingList = State.DataCentersRankingList.Value;
            if (State.DataCentersRankingList.Value.DataCenters.ContainsKey(votingRecord.Option))
            {
                rankingList.DataCenters[votingRecord.Option] =
                    rankingList.DataCenters[votingRecord.Option].Sub(votingRecord.Amount);
                State.DataCentersRankingList.Value = rankingList;
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

        private void CallTokenContractUnlock(Hash input, long amount)
        {
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Address = Context.Sender,
                Symbol = Context.Variables.NativeSymbol,
                Amount = GetElfAmount(amount),
                LockId = input,
                Usage = "Withdraw votes for Main Chain Miner Election."
            });
        }

        private void CallTokenContractTransferFrom(long amount)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Amount = amount,
                Symbol = ElectionContractConstants.VoteSymbol,
                Memo = "Return VOTE tokens."
            });
        }

        private void CallVoteContractWithdraw(Hash input)
        {
            State.VoteContract.Withdraw.Send(new WithdrawInput
            {
                VoteId = input
            });
        }
        private void CallProfitContractRemoveBeneficiary()
        {
            State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
            {
                SchemeId = State.WelfareHash.Value,
                Beneficiary = Context.Sender
            });
        }

        private void AssertValidCandidateInformation(CandidateInformation candidateInformation)
        {
            Assert(candidateInformation != null, "Candidate not found.");
            if (candidateInformation == null) return; // Just to avoid IDE warning.
            Assert(candidateInformation.IsCurrentCandidate, "Candidate quited election.");
        }
        
        private void AssertValidLockSeconds(long lockSeconds)
        {
            Assert(lockSeconds >= State.MinimumLockTime.Value,
                $"Invalid lock time. At least {State.MinimumLockTime.Value.Div(60).Div(60).Div(24)} days");
            Assert(lockSeconds <= State.MaximumLockTime.Value,
                $"Invalid lock time. At most {State.MaximumLockTime.Value.Div(60).Div(60).Div(24)} days");
        }

        private void CallTokenContractLock(long amount)
        {
            State.TokenContract.Lock.Send(new LockInput
            {
                Address = Context.Sender,
                Symbol = Context.Variables.NativeSymbol,
                LockId = Context.TransactionId,
                Amount = GetElfAmount(amount),
                Usage = "Voting for Main Chain Miner Election."
            });
        }

        private void CallTokenContractIssue(long amount)
        {
            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = ElectionContractConstants.VoteSymbol,
                To = Context.Sender,
                Amount = amount,
                Memo = "Issue VOTEs."
            });
        }

        private void CallVoteContractVote(long amount, string candidatePubkey)
        {
            State.VoteContract.Vote.Send(new VoteInput
            {
                Voter = Context.Sender,
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Amount = amount,
                Option = candidatePubkey,
                VoteId = Context.TransactionId
            });
        }

        private void CallProfitContractAddBeneficiary(long votesWeight, long lockSeconds)
        {
            State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
            {
                SchemeId = State.WelfareHash.Value,
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = Context.Sender,
                    Shares = votesWeight
                },
                EndPeriod = GetEndPeriod(lockSeconds)
            });
        }
    }
}