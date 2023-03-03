using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.Treasury;
using AElf.Contracts.Vote;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election;

/// <summary>
///     Vote & Withdraw
/// </summary>
public partial class ElectionContract
{
    private const int DaySec = 86400;

    #region ChangeVotingTarget

    public override Empty ChangeVotingOption(ChangeVotingOptionInput input)
    {
        var targetInformation = State.CandidateInformationMap[input.CandidatePubkey];
        AssertValidCandidateInformation(targetInformation);
        var votingRecord = State.VoteContract.GetVotingRecord.Call(input.VoteId);
        Assert(Context.Sender == votingRecord.Voter, "No permission to change current vote's option.");
        var actualLockedSeconds = Context.CurrentBlockTime.Seconds.Sub(votingRecord.VoteTimestamp.Seconds);
        var claimedLockingSeconds = State.LockTimeMap[input.VoteId];
        Assert(actualLockedSeconds < claimedLockingSeconds, "This vote already expired.");

        if (input.IsResetVotingTime)
        {
            // true for extend EndPeroid of a Profit details, e.g. you vote for 12 months, and on the 6th month, you
            // change the vote, then there will be another 12 months from that time.
            ExtendVoterWelfareProfits(input.VoteId);
        }
        else
        {
            // false, no change for EndPeroid
            State.LockTimeMap[input.VoteId] = State.LockTimeMap[input.VoteId].Sub(actualLockedSeconds);
        }

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
        var oldVoteOptionPublicKey = GetNewestPubkey(votingRecord.Option);
        var oldCandidateVotes = State.CandidateVotes[oldVoteOptionPublicKey];
        oldCandidateVotes.ObtainedActiveVotingRecordIds.Remove(input.VoteId);
        oldCandidateVotes.ObtainedActiveVotedVotesAmount =
            oldCandidateVotes.ObtainedActiveVotedVotesAmount.Sub(votingRecord.Amount);
        oldCandidateVotes.AllObtainedVotedVotesAmount =
            oldCandidateVotes.AllObtainedVotedVotesAmount.Sub(votingRecord.Amount);
        State.CandidateVotes[oldVoteOptionPublicKey] = oldCandidateVotes;

        long voteAmountOfNewCandidate;
        var newCandidateVotes = State.CandidateVotes[input.CandidatePubkey];
        if (newCandidateVotes != null)
        {
            newCandidateVotes.ObtainedActiveVotingRecordIds.Add(input.VoteId);
            newCandidateVotes.ObtainedActiveVotedVotesAmount =
                newCandidateVotes.ObtainedActiveVotedVotesAmount.Add(votingRecord.Amount);
            newCandidateVotes.AllObtainedVotedVotesAmount =
                newCandidateVotes.AllObtainedVotedVotesAmount.Add(votingRecord.Amount);
            State.CandidateVotes[input.CandidatePubkey] = newCandidateVotes;
            voteAmountOfNewCandidate = newCandidateVotes.ObtainedActiveVotedVotesAmount;
        }
        else
        {
            State.CandidateVotes[input.CandidatePubkey] = new CandidateVote
            {
                Pubkey = ByteStringHelper.FromHexString(input.CandidatePubkey),
                ObtainedActiveVotingRecordIds = { input.VoteId },
                ObtainedActiveVotedVotesAmount = votingRecord.Amount,
                AllObtainedVotedVotesAmount = votingRecord.Amount
            };
            voteAmountOfNewCandidate = votingRecord.Amount;
        }

        var dataCenterList = State.DataCentersRankingList.Value;
        if (dataCenterList.DataCenters.ContainsKey(input.CandidatePubkey))
        {
            dataCenterList.DataCenters[input.CandidatePubkey] =
                dataCenterList.DataCenters[input.CandidatePubkey].Add(votingRecord.Amount);
        }
        else if (dataCenterList.DataCenters.Count < GetValidationDataCenterCount())
        {
            // add data center
            dataCenterList.DataCenters.Add(input.CandidatePubkey,
                State.CandidateVotes[input.CandidatePubkey].ObtainedActiveVotedVotesAmount);

            AddBeneficiary(input.CandidatePubkey);
        }
        else
        {
            CandidateReplaceMemberInDataCenter(dataCenterList, input.CandidatePubkey, voteAmountOfNewCandidate);
        }

        if (dataCenterList.DataCenters.ContainsKey(oldVoteOptionPublicKey))
        {
            dataCenterList.DataCenters[oldVoteOptionPublicKey] =
                dataCenterList.DataCenters[oldVoteOptionPublicKey].Sub(votingRecord.Amount);
            UpdateDataCenterAfterMemberVoteAmountChanged(dataCenterList, oldVoteOptionPublicKey);
        }

        State.DataCentersRankingList.Value = dataCenterList;
        return new Empty();
    }

    private void ExtendVoterWelfareProfits(Hash voteId)
    {
        var treasury = State.ProfitContract.GetScheme.Call(State.TreasuryHash.Value);
        var electionVotingRecord = GetElectionVotingRecordByVoteId(voteId);
        
        // Extend endPeriod from now no, so the lockTime will *NOT* be changed.
        var lockTime = State.LockTimeMap[voteId];
        var lockPeriod = lockTime.Div(State.TimeEachTerm.Value);
        if (lockPeriod == 0)
        {
            return;
        }

        var endPeriod = lockPeriod.Add(treasury.CurrentPeriod);
        var extendingDetail = GetProfitDetailByElectionVotingRecord(electionVotingRecord);
        if (extendingDetail != null)
        {
            // The endPeriod is updated and startPeriod is 0, others stay still.
            State.ProfitContract.FixProfitDetail.Send(new FixProfitDetailInput
            {
                SchemeId = State.WelfareHash.Value,
                BeneficiaryShare = new BeneficiaryShare
                {
                    Beneficiary = electionVotingRecord.Voter,
                    Shares = electionVotingRecord.Weight
                },
                EndPeriod = endPeriod,
                ProfitDetailId = voteId
            });
        }
        else
        {
            throw new AssertionException($"Cannot find profit detail of given vote id {voteId}");
        }
    }

    private ElectionVotingRecord GetElectionVotingRecordByVoteId(Hash voteId)
    {
        var votingRecord = State.VoteContract.GetVotingRecord.Call(voteId);
        return TransferVotingRecordToElectionVotingRecord(votingRecord, voteId);
    }

    private ProfitDetail GetProfitDetailByElectionVotingRecord(ElectionVotingRecord electionVotingRecord)
    {
        var profitDetails = State.ProfitContract.GetProfitDetails.Call(new GetProfitDetailsInput
        {
            Beneficiary = electionVotingRecord.Voter,
            SchemeId = State.WelfareHash.Value
        });

        // In new rules, profitDetail.Id equals to its vote id.
        ProfitDetail profitDetail = profitDetails.Details.FirstOrDefault(d => d.Id == electionVotingRecord.VoteId);
        // However, in the old world, profitDetail.Id is null, so use Shares.
        if (profitDetail == null)
        {
            profitDetail = profitDetails.Details.LastOrDefault(d => d.Shares == electionVotingRecord.Weight);
        }
        
        return profitDetail;
    }

    #endregion

    public override Empty SetVoteWeightInterest(VoteWeightInterestList input)
    {
        AssertPerformedByVoteWeightInterestController();
        Assert(input.VoteWeightInterestInfos.Count > 0, "invalid input");
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
        Assert(input.TimeProportion > 0 && input.AmountProportion > 0, "invalid input");
        State.VoteWeightProportion.Value = input;
        return new Empty();
    }

    public override Empty ChangeVoteWeightInterestController(AuthorityInfo input)
    {
        AssertPerformedByVoteWeightInterestController();
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

    private void UnlockTokensOfVoter(Hash input, long amount, Address voterAddress = null)
    {
        State.TokenContract.Unlock.Send(new UnlockInput
        {
            Address = voterAddress ?? Context.Sender,
            Symbol = Context.Variables.NativeSymbol,
            Amount = amount,
            LockId = input,
            Usage = "Withdraw votes for Main Chain Miner Election."
        });
    }

    private void RetrieveTokensFromVoter(long amount, Address voterAddress = null)
    {
        foreach (var symbol in new List<string>
                     { ElectionContractConstants.ShareSymbol, ElectionContractConstants.VoteSymbol })
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = voterAddress ?? Context.Sender,
                To = Context.Self,
                Amount = amount,
                Symbol = symbol,
                Memo = $"Return {symbol} tokens."
            });
    }

    private void WithdrawTokensOfVoter(Hash input)
    {
        State.VoteContract.Withdraw.Send(new WithdrawInput
        {
            VoteId = input
        });
    }

    private void RemoveBeneficiaryOfVoter(Address voterAddress = null)
    {
        State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
        {
            SchemeId = State.WelfareHash.Value,
            Beneficiary = voterAddress ?? Context.Sender
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
    ///     Issue VOTE tokens to this voter.
    /// </summary>
    /// <param name="amount"></param>
    private void TransferTokensToVoter(long amount)
    {
        foreach (var symbol in new List<string>
                     { ElectionContractConstants.ShareSymbol, ElectionContractConstants.VoteSymbol })
            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = symbol,
                To = Context.Sender,
                Amount = amount,
                Memo = $"Transfer {symbol}."
            });
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

    private void AddBeneficiaryToVoter(long votesWeight, long lockSeconds, Hash voteId)
    {
        State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
        {
            SchemeId = State.WelfareHash.Value,
            BeneficiaryShare = new BeneficiaryShare
            {
                Beneficiary = Context.Sender,
                Shares = votesWeight
            },
            EndPeriod = GetEndPeriod(lockSeconds),
            // one vote, one profit detail, so voteId equals to profitDetailId
            ProfitDetailId = voteId
        });
    }

    private void AssertPerformedByVoteWeightInterestController()
    {
        if (State.VoteWeightInterestController.Value == null)
            State.VoteWeightInterestController.Value = GetDefaultVoteWeightInterestController();

        Assert(Context.Sender == State.VoteWeightInterestController.Value.OwnerAddress, "No permission.");
    }

    private AuthorityInfo GetDefaultVoteWeightInterestController()
    {
        return new AuthorityInfo
        {
            ContractAddress = Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName),
            OwnerAddress = GetParliamentDefaultAddress()
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

    #region Vote

    /// <summary>
    ///     Call the Vote function of VoteContract to do a voting.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public override Hash Vote(VoteMinerInput input)
    {
        // Check candidate information map instead of candidates. 
        var targetInformation = State.CandidateInformationMap[input.CandidatePubkey];
        AssertValidCandidateInformation(targetInformation);

        var electorPubkey = Context.RecoverPublicKey();

        var lockSeconds = (input.EndTimestamp - Context.CurrentBlockTime).Seconds;
        AssertValidLockSeconds(lockSeconds);

        var voteId = GenerateVoteId(input);
        Assert(State.LockTimeMap[voteId] == 0, "Vote already exists.");
        State.LockTimeMap[voteId] = lockSeconds;

        UpdateElectorInformation(electorPubkey, input.Amount, voteId);

        var candidateVotesAmount = UpdateCandidateInformation(input.CandidatePubkey, input.Amount, voteId);

        LockTokensOfVoter(input.Amount, voteId);
        TransferTokensToVoter(input.Amount);
        CallVoteContractVote(input.Amount, input.CandidatePubkey, voteId);
        AddBeneficiaryToVoter(GetVotesWeight(input.Amount, lockSeconds), lockSeconds, voteId);

        var rankingList = State.DataCentersRankingList.Value;
        if (rankingList.DataCenters.ContainsKey(input.CandidatePubkey))
        {
            rankingList.DataCenters[input.CandidatePubkey] =
                rankingList.DataCenters[input.CandidatePubkey].Add(input.Amount);
            State.DataCentersRankingList.Value = rankingList;
        }
        else
        {
            if (rankingList.DataCenters.Count < GetValidationDataCenterCount())
            {
                State.DataCentersRankingList.Value.DataCenters.Add(input.CandidatePubkey,
                    candidateVotesAmount);
                AddBeneficiary(input.CandidatePubkey);
            }
            else
            {
                TryToBecomeAValidationDataCenter(input, candidateVotesAmount, rankingList);
            }
        }

        return voteId;
    }

    private void TryToBecomeAValidationDataCenter(VoteMinerInput input, long candidateVotesAmount,
        DataCenterRankingList rankingList)
    {
        var minimumVotes = candidateVotesAmount;
        var minimumVotesCandidate = input.CandidatePubkey;
        var replaceWillHappen = false;
        foreach (var pubkeyToVotesAmount in rankingList.DataCenters.OrderBy(x => x.Value))
            if (pubkeyToVotesAmount.Value < minimumVotes)
            {
                replaceWillHappen = true;
                minimumVotesCandidate = pubkeyToVotesAmount.Key;
                break;
            }

        if (replaceWillHappen)
        {
            State.DataCentersRankingList.Value.DataCenters.Remove(minimumVotesCandidate);
            State.DataCentersRankingList.Value.DataCenters.Add(input.CandidatePubkey,
                candidateVotesAmount);
            NotifyProfitReplaceCandidateInDataCenter(minimumVotesCandidate, input.CandidatePubkey);
        }
    }

    /// <summary>
    ///     Update Elector's Votes information.
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
                ActiveVotingRecordIds = { voteId },
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
    ///     Update Candidate's Votes information.
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
                Pubkey = ByteStringHelper.FromHexString(candidatePublicKey),
                ObtainedActiveVotingRecordIds = { voteId },
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
            var initBase = 1 + (decimal)instMap.Interest / instMap.Capital;
            return ((long)(Pow(initBase, (uint)lockDays) * votesAmount)).Add(votesAmount
                .Mul(timeAndAmountProportion.AmountProportion).Div(timeAndAmountProportion.TimeProportion));
        }

        var maxInterestInfo = State.VoteWeightInterestList.Value.VoteWeightInterestInfos.Last();
        var maxInterestBase = 1 + (decimal)maxInterestInfo.Interest / maxInterestInfo.Capital;
        return ((long)(Pow(maxInterestBase, (uint)lockDays) * votesAmount)).Add(votesAmount
            .Mul(timeAndAmountProportion.AmountProportion).Div(timeAndAmountProportion.TimeProportion));
    }

    private static decimal Pow(decimal x, uint y)
    {
        if (y == 1)
            return (long)x;
        var a = 1m;
        if (y == 0)
            return a;
        var e = new BitArray(y.ToBytes(false));
        var t = e.Count;
        for (var i = t - 1; i >= 0; --i)
        {
            a *= a;
            if (e[i]) a *= x;
        }

        return a;
    }

    private long GetEndPeriod(long lockTime)
    {
        var treasury = State.ProfitContract.GetScheme.Call(State.TreasuryHash.Value);
        return lockTime.Div(State.TimeEachTerm.Value).Add(treasury.CurrentPeriod);
    }

    #endregion

    #region Withdraw

    /// <summary>
    ///     Withdraw a voting,recall the votes the voted by sender.
    ///     At the same time,the Shares that the voter occupied will sub form TotalShares.
    ///     and the "VOTE" token will be returned to ElectionContract;
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
        if (voterVotes == null) throw new AssertionException($"Voter {voterPublicKey} never votes before.");

        voterVotes.ActiveVotingRecordIds.Remove(input);
        voterVotes.WithdrawnVotingRecordIds.Add(input);
        voterVotes.ActiveVotedVotesAmount = voterVotes.ActiveVotedVotesAmount.Sub(votingRecord.Amount);
        State.ElectorVotes[voterPublicKey] = voterVotes;

        // Update Candidate's Votes information.
        var newestPubkey = GetNewestPubkey(votingRecord.Option);
        var candidateVotes = State.CandidateVotes[newestPubkey];
        if (candidateVotes == null)
            throw new AssertionException(
                $"Newest pubkey {newestPubkey} is invalid. Old pubkey is {votingRecord.Option}");

        candidateVotes.ObtainedActiveVotingRecordIds.Remove(input);
        candidateVotes.ObtainedWithdrawnVotingRecordIds.Add(input);
        candidateVotes.ObtainedActiveVotedVotesAmount =
            candidateVotes.ObtainedActiveVotedVotesAmount.Sub(votingRecord.Amount);
        State.CandidateVotes[newestPubkey] = candidateVotes;

        UnlockTokensOfVoter(input, votingRecord.Amount);
        RetrieveTokensFromVoter(votingRecord.Amount);
        WithdrawTokensOfVoter(input);
        if (!State.WeightsAlreadyFixedMap[input])
        {
            RemoveBeneficiaryOfVoter();
            State.WeightsAlreadyFixedMap.Remove(input);
        }

        var rankingList = State.DataCentersRankingList.Value;
        if (!rankingList.DataCenters.ContainsKey(newestPubkey)) return new Empty();
        rankingList.DataCenters[newestPubkey] =
            rankingList.DataCenters[newestPubkey].Sub(votingRecord.Amount);
        UpdateDataCenterAfterMemberVoteAmountChanged(rankingList, newestPubkey);
        State.DataCentersRankingList.Value = rankingList;

        return new Empty();
    }

    private void UpdateDataCenterAfterMemberVoteAmountChanged(DataCenterRankingList rankingList,
        string targetMember,
        bool isForceReplace = false)
    {
        var amountAfterWithdraw = rankingList.DataCenters[targetMember];
        if (isForceReplace)
            Assert(amountAfterWithdraw == 0, "should update vote amount in data center firstly");
        else if (rankingList.DataCenters.Any(x => x.Value < amountAfterWithdraw))
            return;

        var validCandidates = State.Candidates.Value.Value.Select(x => x.ToHex())
            .Where(c => !rankingList.DataCenters.ContainsKey(c) && State.CandidateVotes[c] != null)
            .OrderByDescending(x => State.CandidateVotes[x].ObtainedActiveVotedVotesAmount);
        string maxVoterPublicKeyStringOutOfDataCenter = null;
        long maxVoteAmountOutOfDataCenter = 0;
        var maxVoteCandidateOutDataCenter = validCandidates.FirstOrDefault();
        if (maxVoteCandidateOutDataCenter != null)
        {
            maxVoterPublicKeyStringOutOfDataCenter = maxVoteCandidateOutDataCenter;
            maxVoteAmountOutOfDataCenter = State.CandidateVotes[maxVoteCandidateOutDataCenter]
                .ObtainedActiveVotedVotesAmount;
        }

        if (isForceReplace)
        {
            rankingList.DataCenters.Remove(targetMember);
            if (maxVoteCandidateOutDataCenter == null)
            {
                maxVoteCandidateOutDataCenter = State.Candidates.Value.Value.Select(x => x.ToHex())
                    .FirstOrDefault(c =>
                        !rankingList.DataCenters.ContainsKey(c) && State.CandidateVotes[c] == null);
                if (maxVoteCandidateOutDataCenter != null)
                {
                    maxVoterPublicKeyStringOutOfDataCenter = maxVoteCandidateOutDataCenter;
                    maxVoteAmountOutOfDataCenter = 0;
                }
            }
        }
        else
        {
            if (maxVoteAmountOutOfDataCenter <= amountAfterWithdraw)
                return;
            rankingList.DataCenters.Remove(targetMember);
        }

        if (maxVoterPublicKeyStringOutOfDataCenter != null)
            rankingList.DataCenters[maxVoterPublicKeyStringOutOfDataCenter] = maxVoteAmountOutOfDataCenter;

        NotifyProfitReplaceCandidateInDataCenter(targetMember, maxVoterPublicKeyStringOutOfDataCenter);
    }

    private void CandidateReplaceMemberInDataCenter(DataCenterRankingList rankingList, string candidate,
        long voteAmount)
    {
        var dateCenter = rankingList.DataCenters;
        if (dateCenter.Count < GetValidationDataCenterCount())
            return;
        if (dateCenter.ContainsKey(candidate))
            return;
        var list = dateCenter.ToList();
        var minimumVoteCandidateInDataCenter = list.OrderBy(x => x.Value).First();
        if (voteAmount <= minimumVoteCandidateInDataCenter.Value) return;
        dateCenter.Remove(minimumVoteCandidateInDataCenter.Key);
        dateCenter[candidate] = voteAmount;
        NotifyProfitReplaceCandidateInDataCenter(minimumVoteCandidateInDataCenter.Key, candidate);
    }

    private void NotifyProfitReplaceCandidateInDataCenter(string oldCandidateInDataCenter,
        string newCandidateDataCenter)
    {
        RemoveBeneficiary(oldCandidateInDataCenter);

        if (newCandidateDataCenter == null)
            return;

        AddBeneficiary(newCandidateDataCenter);
    }

    #endregion

    #region subsidy helper

    private Hash GenerateSubsidyId(string pubkey,Address beneficiaryAddress)
    {
        return HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(pubkey), HashHelper.ComputeFrom(beneficiaryAddress),
            HashHelper.ComputeFrom(Context.Self));
    }
    
    private Address GetProfitsReceiverOrDefault(string pubkey)
    {
        if (State.TreasuryContract.Value == null)
            State.TreasuryContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.TreasuryContractSystemName);
        var address = State.TreasuryContract.GetProfitsReceiverOrDefault.Call(new StringValue
        {
            Value = pubkey
        });
        return address;
    }

    private void AddBeneficiary(string candidatePubkey, Address profitsReceiver = null)
    {
        var beneficiaryAddress = GetBeneficiaryAddress(candidatePubkey, profitsReceiver);
        var subsidyId = GenerateSubsidyId(candidatePubkey,beneficiaryAddress);
        State.ProfitContract.AddBeneficiary.Send(new AddBeneficiaryInput
        {
            SchemeId = State.SubsidyHash.Value,
            BeneficiaryShare = new BeneficiaryShare
            {
                Beneficiary = beneficiaryAddress,
                Shares = 1,
            },
            ProfitDetailId = subsidyId
        });
    }

    private void RemoveBeneficiary(string candidatePubkey,Address profitsReceiver = null)
    {
        var beneficiaryAddress = GetBeneficiaryAddress(candidatePubkey, profitsReceiver);
        var previousSubsidyId = GenerateSubsidyId(candidatePubkey,beneficiaryAddress);
        State.ProfitContract.RemoveBeneficiary.Send(new RemoveBeneficiaryInput
        {
            SchemeId = State.SubsidyHash.Value,
            Beneficiary = beneficiaryAddress,
            ProfitDetailId = previousSubsidyId
        });
    }

    private Address GetBeneficiaryAddress(string candidatePubkey, Address profitsReceiver = null)
    {
        profitsReceiver = profitsReceiver == null ? GetProfitsReceiverOrDefault(candidatePubkey) : profitsReceiver;
        var beneficiaryAddress = profitsReceiver.Value.Any()
            ? profitsReceiver
            : Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(candidatePubkey));
        return beneficiaryAddress;
    }
    
    #endregion
}