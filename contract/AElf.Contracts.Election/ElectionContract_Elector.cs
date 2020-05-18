using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Acs1;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.CSharp.Core;
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
        private const int DaySec = 86400;

        #region Vote

        /// <summary>
        /// Call the Vote function of VoteContract to do a voting.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Hash Vote(VoteMinerInput input)
        {
            // Check candidate information map instead of candidates. 
            var targetInformation = State.CandidateInformationMap[input.CandidatePubkey];
            AssertValidCandidateInformation(targetInformation);

            var lockSeconds = (input.EndTimestamp - Context.CurrentBlockTime).Seconds;
            AssertValidLockSeconds(lockSeconds);

            var voteId = GenerateVoteId(input);
            Assert(State.LockTimeMap[voteId] == 0, "Vote already exists.");
            State.LockTimeMap[voteId] = lockSeconds;

            var recoveredPublicKey = Context.RecoverPublicKey();

            UpdateElectorInformation(recoveredPublicKey, input.Amount, voteId);

            var candidateVotesAmount = UpdateCandidateInformation(input.CandidatePubkey, input.Amount, voteId);

            LockTokensOfVoter(input.Amount, voteId);
            IssueOrTransferTokensToVoter(input.Amount);
            CallVoteContractVote(input.Amount, input.CandidatePubkey, voteId);
            AddBeneficiaryToVoter(GetVotesWeight(input.Amount, lockSeconds), lockSeconds);

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

            return voteId;
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
        /// <param name="voteId"></param>
        private void UpdateElectorInformation(byte[] recoveredPublicKey, long amount, Hash voteId)
        {
            var voterPublicKey = recoveredPublicKey.ToHex();
            var voterPublicKeyByteString = ByteString.CopyFrom(recoveredPublicKey);
            var voterVotes = State.ElectorVotes[voterPublicKey];
            if (voterVotes == null)
            {
                voterVotes = new ElectorVote
                {
                    Pubkey = voterPublicKeyByteString,
                    ActiveVotingRecordIds = {voteId},
                    ActiveVotedVotesAmount = amount,
                    AllVotedVotesAmount = amount
                };
            }
            else
            {
                voterVotes.ActiveVotingRecordIds.Add(voteId);
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
        /// <param name="voteId"></param>
        private long UpdateCandidateInformation(string candidatePublicKey, long amount, Hash voteId)
        {
            var candidateVotes = State.CandidateVotes[candidatePublicKey];
            if (candidateVotes == null)
            {
                candidateVotes = new CandidateVote
                {
                    Pubkey = candidatePublicKey.ToByteString(),
                    ObtainedActiveVotingRecordIds = {voteId},
                    ObtainedActiveVotedVotesAmount = amount,
                    AllObtainedVotedVotesAmount = amount
                };
            }
            else
            {
                candidateVotes.ObtainedActiveVotingRecordIds.Add(voteId);
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
            var lockDays = lockTime.Div(DaySec);
            var timeAndAmountProportion = GetVoteWeightProportion();
            if (State.VoteWeightInterestList.Value == null)
                State.VoteWeightInterestList.Value = GetDefaultVoteWeightInterest();
            foreach (var instMap in State.VoteWeightInterestList.Value.VoteWeightInterestInfos)
            {
                if (lockDays > instMap.Day)
                    continue;
                var initBase = 1 + (decimal) instMap.Interest / instMap.Capital;
                return ((long) (Pow(initBase, (uint) lockDays) * votesAmount)).Add(votesAmount
                    .Mul(timeAndAmountProportion.AmountProportion).Div(timeAndAmountProportion.TimeProportion));
            }

            var maxInterestInfo = State.VoteWeightInterestList.Value.VoteWeightInterestInfos.Last();
            var maxInterestBase = 1 + (decimal) maxInterestInfo.Interest / maxInterestInfo.Capital;
            return ((long) (Pow(maxInterestBase, (uint) lockDays) * votesAmount)).Add(votesAmount
                .Mul(timeAndAmountProportion.AmountProportion).Div(timeAndAmountProportion.TimeProportion));
        }

        private static decimal Pow(decimal x, uint y)
        {
            if (y == 1)
                return (long) x;
            decimal a = 1m;
            if (y == 0)
                return a;
            var e = new BitArray(y.ToBytes(false));
            var t = e.Count;
            for (var i = t - 1; i >= 0; --i)
            {
                a *= a;
                if (e[i])
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
            Assert(Context.Sender == votingRecord.Voter, "No permission to change current vote's option.");
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
            oldCandidateVotes.AllObtainedVotedVotesAmount =
                oldCandidateVotes.AllObtainedVotedVotesAmount.Sub(votingRecord.Amount);
            State.CandidateVotes[votingRecord.Option] = oldCandidateVotes;

            var newCandidateVotes = State.CandidateVotes[input.CandidatePubkey];
            if (newCandidateVotes != null)
            {
                newCandidateVotes.ObtainedActiveVotingRecordIds.Add(input.VoteId);
                newCandidateVotes.ObtainedActiveVotedVotesAmount =
                    newCandidateVotes.ObtainedActiveVotedVotesAmount.Add(votingRecord.Amount);
                newCandidateVotes.AllObtainedVotedVotesAmount =
                    newCandidateVotes.AllObtainedVotedVotesAmount.Add(votingRecord.Amount);
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
            candidateVotes.ObtainedActiveVotedVotesAmount =
                candidateVotes.ObtainedActiveVotedVotesAmount.Sub(votingRecord.Amount);
            State.CandidateVotes[votingRecord.Option] = candidateVotes;

            UnlockTokensOfVoter(input, votingRecord.Amount);
            RetrieveTokensFromVoter(votingRecord.Amount);
            WithdrawTokensOfVoter(input);
            RemoveBeneficiaryOfVoter();

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

        public override Empty SetVoteWeightInterest(VoteWeightInterestList input)
        {
            AssertPerformedByVoteWeightInterestController();
            Assert(input != null && input.VoteWeightInterestInfos.Count > 0, "invalid input");
            // ReSharper disable once PossibleNullReferenceException
            foreach (var info in input.VoteWeightInterestInfos)
            {
                Assert(info.Capital > 0, "invalid input");
                Assert(info.Day > 0, "invalid input");
                Assert(info.Interest > 0, "invalid input");
            }

            Assert(input.VoteWeightInterestInfos.GroupBy(x => x.Day).Count() == input.VoteWeightInterestInfos.Count,
                "repeat day input");
            var orderList = input.VoteWeightInterestInfos.OrderBy(x => x.Day).ToArray();
            input.VoteWeightInterestInfos.Clear();
            input.VoteWeightInterestInfos.AddRange(orderList);
            State.VoteWeightInterestList.Value = input;
            return new Empty();
        }
        
        public override Empty SetVoteWeightProportion(VoteWeightProportion input)
        {
            AssertPerformedByVoteWeightInterestController();
            Assert(input != null && input.TimeProportion > 0 && input.AmountProportion > 0, "invalid input");
            State.VoteWeightProportion.Value = input;
            return new Empty();
        }

        public override Empty ChangeVoteWeightInterestController(AuthorityInfo input)
        {
            AssertPerformedByVoteWeightInterestController();
            Assert(input != null, "invalid input");
            Assert(CheckOrganizationExist(input), "Invalid authority input.");
            State.VoteWeightInterestController.Value = input;
            return new Empty();
        }

        private VoteWeightInterestList GetDefaultVoteWeightInterest()
        {
            return new VoteWeightInterestList
            {
                VoteWeightInterestInfos =
                {
                    new VoteWeightInterest
                    {
                        Day = 365,
                        Interest = 1,
                        Capital = 1000
                    },
                    new VoteWeightInterest
                    {
                        Day = 730,
                        Interest = 15,
                        Capital = 10000
                    },
                    new VoteWeightInterest
                    {
                        Day = 1095,
                        Interest = 2,
                        Capital = 1000
                    }
                }
            };
        }
        
        private VoteWeightProportion GetVoteWeightProportion()
        {
            return State.VoteWeightProportion.Value ??
                   (State.VoteWeightProportion.Value = GetDefaultVoteWeightProportion());
        }
        private VoteWeightProportion GetDefaultVoteWeightProportion()
        {
            return new VoteWeightProportion
            {
                TimeProportion = 2,
                AmountProportion = 1
            };
        }

        private void UnlockTokensOfVoter(Hash input, long amount)
        {
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                Address = Context.Sender,
                Symbol = Context.Variables.NativeSymbol,
                Amount = amount,
                LockId = input,
                Usage = "Withdraw votes for Main Chain Miner Election."
            });
        }

        private void RetrieveTokensFromVoter(long amount)
        {
            foreach (var symbol in new List<string>
                {ElectionContractConstants.ShareSymbol, ElectionContractConstants.VoteSymbol})
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = Context.Self,
                    Amount = amount,
                    Symbol = symbol,
                    Memo = $"Return {symbol} tokens."
                });
            }
        }

        private void WithdrawTokensOfVoter(Hash input)
        {
            State.VoteContract.Withdraw.Send(new WithdrawInput
            {
                VoteId = input
            });
        }

        private void RemoveBeneficiaryOfVoter()
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
            // ReSharper disable once PossibleNullReferenceException
            Assert(candidateInformation.IsCurrentCandidate, "Candidate quited election.");
        }

        private void AssertValidLockSeconds(long lockSeconds)
        {
            Assert(lockSeconds >= State.MinimumLockTime.Value,
                $"Invalid lock time. At least {State.MinimumLockTime.Value.Div(60).Div(60).Div(24)} days");
            Assert(lockSeconds <= State.MaximumLockTime.Value,
                $"Invalid lock time. At most {State.MaximumLockTime.Value.Div(60).Div(60).Div(24)} days");
        }

        private void LockTokensOfVoter(long amount, Hash voteId)
        {
            State.TokenContract.Lock.Send(new LockInput
            {
                Address = Context.Sender,
                Symbol = Context.Variables.NativeSymbol,
                LockId = voteId,
                Amount = amount,
                Usage = "Voting for Main Chain Miner Election."
            });
        }

        /// <summary>
        /// Issue VOTE tokens to this voter.
        /// </summary>
        /// <param name="amount"></param>
        private void IssueOrTransferTokensToVoter(long amount)
        {
            foreach (var symbol in new List<string>
                {ElectionContractConstants.ShareSymbol, ElectionContractConstants.VoteSymbol})
            {
                var tokenInfo = State.TokenContract.GetTokenInfo.Call(new GetTokenInfoInput
                {
                    Symbol = symbol
                });
                if (tokenInfo.TotalSupply.Sub(tokenInfo.Supply) <= amount) // Which means remain tokens not enough.
                {
                    State.TokenContract.Transfer.Send(new TransferInput
                    {
                        Symbol = symbol,
                        To = Context.Sender,
                        Amount = amount,
                        Memo = $"Transfer {symbol}."
                    });
                }
                else
                {
                    State.TokenContract.Issue.Send(new IssueInput
                    {
                        Symbol = symbol,
                        To = Context.Sender,
                        Amount = amount,
                        Memo = $"Issue {symbol}."
                    });
                }
            }
        }

        private void CallVoteContractVote(long amount, string candidatePubkey, Hash voteId)
        {
            State.VoteContract.Vote.Send(new VoteInput
            {
                Voter = Context.Sender,
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Amount = amount,
                Option = candidatePubkey,
                VoteId = voteId
            });
        }

        private void AddBeneficiaryToVoter(long votesWeight, long lockSeconds)
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

        private void AssertPerformedByVoteWeightInterestController()
        {
            if (State.VoteWeightInterestController.Value == null)
            {
                State.VoteWeightInterestController.Value = GetDefaultVoteWeightInterestController();
            }

            Assert(Context.Sender == State.VoteWeightInterestController.Value.OwnerAddress, "No permission.");
        }

        private AuthorityInfo GetDefaultVoteWeightInterestController()
        {
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }

            return new AuthorityInfo
            {
                ContractAddress = State.ParliamentContract.Value,
                OwnerAddress = State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty())
            };
        }

        private Hash GenerateVoteId(VoteMinerInput voteMinerInput)
        {
            if (voteMinerInput.Token != null)
                return Context.GenerateId(Context.Self, voteMinerInput.Token);
            
            var candidateVotesCount =
                State.CandidateVotes[voteMinerInput.CandidatePubkey]?.ObtainedActiveVotedVotesAmount ?? 0;
            return Context.GenerateId(Context.Self,
                ByteArrayHelper.ConcatArrays(voteMinerInput.CandidatePubkey.GetBytes(),
                    candidateVotesCount.ToBytes(false)));
        }
    }
}