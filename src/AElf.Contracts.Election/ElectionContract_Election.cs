using System;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Vote;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract : ElectionContractContainer.ElectionContractBase
    {
        public override Empty InitialElectionContract(InitialElectionContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.VoteContractSystemName.Value = input.VoteContractSystemName;
            State.ProfitContractSystemName.Value = input.ProfitContractSystemName;
            State.TokenContractSystemName.Value = input.TokenContractSystemName;
            State.AElfConsensusContractSystemName.Value = input.AelfConsensusContractSystemName;
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
            State.Candidates.Value = new PublicKeysList();
            State.MinimumLockTime.Value = input.MinimumLockTime;
            State.MaximumLockTime.Value = input.MaximumLockTime;
            State.Initialized.Value = true;
            State.BlackList.Value = new PublicKeysList();
            return new Empty();
        }

        public override Empty SetInitialMiners(PublicKeysList input)
        {
            State.AElfConsensusContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.AElfConsensusContractSystemName.Value);
            Assert(State.AElfConsensusContract.Value == Context.Sender, "Only Consensus Contract can call this method.");
            Assert(State.InitialMiners.Value == null, "Initial miners already set.");
            State.InitialMiners.Value = new PublicKeysList {Value = {input.Value}};
            foreach (var publicKey in input.Value)
            {
                State.CandidateInformationMap[publicKey.ToHex()] = new CandidateInformation {PublicKey = publicKey.ToHex()};
            }

            State.MinersCount.Value = input.Value.Count;
            return new Empty();
        }

        public override Empty RegisterElectionVotingEvent(Empty input)
        {
            Assert(!State.VotingEventRegistered.Value, "Already registered.");
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();

            State.TokenContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.TokenContractSystemName.Value);
            State.VoteContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.VoteContractSystemName.Value);
            State.AElfConsensusContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.AElfConsensusContractSystemName.Value);

            Context.LogDebug(() =>
                $"Will change term every {Context.Variables.TimeEachTerm} Days");
            Context.LogDebug(() => $"Minimum lock time: {State.MinimumLockTime.Value} {(TimeUnit) State.BaseTimeUnit.Value}");
            Context.LogDebug(() => $"Maximum lock time: {State.MaximumLockTime.Value} {(TimeUnit) State.BaseTimeUnit.Value}");

            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = ElectionContractConstants.VoteSymbol,
                TokenName = "Vote token",
                Issuer = Context.Self,
                Decimals = 2,
                IsBurnable = true,
                TotalSupply = ElectionContractConstants.VotesTotalSupply,
                LockWhiteList = {Context.Self}
            });

            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = ElectionContractConstants.VoteSymbol,
                Amount = ElectionContractConstants.VotesTotalSupply,
                To = Context.Self,
                Memo = "Power!"
            });

            var votingRegisterInput = new VotingRegisterInput
            {
                IsLockToken = false,
                AcceptedCurrency = Context.Variables.NativeSymbol,
                TotalSnapshotNumber = long.MaxValue,
                StartTimestamp = DateTime.MinValue.ToUniversalTime().ToTimestamp(),
                EndTimestamp = DateTime.MaxValue.ToUniversalTime().ToTimestamp()
            };
            State.VoteContract.Register.Send(votingRegisterInput);

            State.MinerElectionVotingItemId.Value = Hash.FromTwoHashes(Hash.FromMessage(votingRegisterInput),
                Hash.FromMessage(Context.Self));

            State.VotingEventRegistered.Value = true;
            return new Empty();
        }

        // TODO: Consider a limit amount of candidates.
        /// <summary>
        /// Actually this method is for adding an option of voting.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty AnnounceElection(Empty input)
        {
            var publicKey = Context.RecoverPublicKey().ToHex();
            var publicKeyByteString = ByteString.CopyFrom(Context.RecoverPublicKey());

            Assert(
                State.ElectorVotes[publicKey] == null || State.ElectorVotes[publicKey].ActiveVotingRecordIds == null ||
                State.ElectorVotes[publicKey].ActiveVotedVotesAmount == 0, "Voter can't announce election.");

            Assert(!State.InitialMiners.Value.Value.Contains(publicKeyByteString),
                "Initial miner cannot announce election.");

            var candidateInformation = State.CandidateInformationMap[publicKey];
            
            if (candidateInformation != null)
            {
                Assert(!candidateInformation.IsCurrentCandidate,
                    "This public key already announced election.");
                candidateInformation.AnnouncementTransactionId = Context.TransactionId;
                candidateInformation.IsCurrentCandidate = true;
                State.CandidateInformationMap[publicKey] = candidateInformation;
            }
            else
            {
                Assert(!State.BlackList.Value.Value.Contains(publicKeyByteString),
                    "This candidate already marked as evil node before.");
                State.CandidateInformationMap[publicKey] = new CandidateInformation
                {
                    PublicKey = publicKey,
                    AnnouncementTransactionId = Context.TransactionId,
                    IsCurrentCandidate = true
                };
            }

            State.Candidates.Value.Value.Add(publicKeyByteString);

            State.TokenContract.Lock.Send(new LockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = Context.Variables.NativeSymbol,
                Amount = ElectionContractConstants.LockTokenForElection,
                LockId = Context.TransactionId,
                Usage = "Lock for announcing election."
            });

            State.VoteContract.AddOption.Send(new AddOptionInput
            {
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Option = publicKey
            });

            State.ProfitContract.AddWeight.Send(new AddWeightInput
            {
                ProfitId = State.SubsidyHash.Value,
                Receiver = Context.Sender,
                Weight = 1
            });

            return new Empty();
        }

        public override Empty QuitElection(Empty input)
        {
            var publicKey = Context.RecoverPublicKey().ToHex();
            var publicKeyByteString = ByteString.CopyFrom(Context.RecoverPublicKey());

            Assert(State.Candidates.Value.Value.Contains(publicKeyByteString), "Sender is not a candidate.");
            Assert(
                !State.AElfConsensusContract.GetCurrentMiners.Call(new Empty()).PublicKeys
                    .Contains(publicKeyByteString),
                "Current miners cannot quit election.");

            var candidateInformation = State.CandidateInformationMap[publicKey];

            State.Candidates.Value.Value.Remove(publicKeyByteString);
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = Context.Variables.NativeSymbol,
                LockId = candidateInformation.AnnouncementTransactionId,
                Amount = ElectionContractConstants.LockTokenForElection,
                Usage = "Quit election."
            });

            State.VoteContract.RemoveOption.Send(new RemoveOptionInput
            {
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Option = publicKey
            });

            candidateInformation.IsCurrentCandidate = false;
            candidateInformation.AnnouncementTransactionId = Hash.Empty;
            State.CandidateInformationMap[publicKey] = candidateInformation;

            State.ProfitContract.SubWeight.Send(new SubWeightInput
            {
                ProfitId = State.SubsidyHash.Value,
                Receiver = Context.Sender
            });

            return new Empty();
        }

        public override Empty Vote(VoteMinerInput input)
        {
            Assert(State.CandidateInformationMap[input.CandidatePublicKey] != null, "Candidate not found.");
            Assert(State.CandidateInformationMap[input.CandidatePublicKey].IsCurrentCandidate,
                "Candidate quited election.");

            var lockDays = (input.EndTimestamp - Context.CurrentBlockTime.ToTimestamp()).ToTimeSpan().TotalDays;
            Assert(lockDays >= State.MinimumLockTime.Value,
                $"Invalid lock time. At least {State.MinimumLockTime.Value} {(TimeUnit) State.BaseTimeUnit.Value}");
            Assert(lockDays <= State.MaximumLockTime.Value,
                $"Invalid lock time. At most {State.MaximumLockTime.Value} {(TimeUnit) State.BaseTimeUnit.Value}");

            State.LockTimeMap[Context.TransactionId] =
                GetTimeSpan(input.EndTimestamp.ToDateTime(), Context.CurrentBlockTime);

            // Update Voter's Votes information.
            var voterPublicKeyBytes = Context.RecoverPublicKey();
            var voterPublicKey = voterPublicKeyBytes.ToHex();
            var voterPublicKeyByteString = ByteString.CopyFrom(voterPublicKeyBytes);
            var voterVotes = State.ElectorVotes[voterPublicKey];
            if (voterVotes == null)
            {
                voterVotes = new ElectorVote
                {
                    PublicKey = voterPublicKeyByteString,
                    ActiveVotingRecordIds = { Context.TransactionId},
                    ActiveVotedVotesAmount = input.Amount,
                    AllVotedVotesAmount = input.Amount
                };
            }
            else
            {
                voterVotes.ActiveVotingRecordIds.Add(Context.TransactionId);
                voterVotes.ActiveVotedVotesAmount = voterVotes.ActiveVotedVotesAmount.Add(input.Amount);
                voterVotes.AllVotedVotesAmount = voterVotes.AllVotedVotesAmount.Add(input.Amount);
            }

            State.ElectorVotes[voterPublicKey] = voterVotes;

            // Update Candidate's Votes information.
            var candidateVotes = State.CandidateVotes[input.CandidatePublicKey];
            if (candidateVotes == null)
            {
                candidateVotes = new CandidateVote
                {
                    PublicKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.CandidatePublicKey)),
                    ObtainedActiveVotingRecordIds = { Context.TransactionId},
                    ObtainedActiveVotedVotesAmount = input.Amount,
                    AllObtainedVotedVotesAmount = input.Amount
                };
            }
            else
            {
                candidateVotes.ObtainedActiveVotingRecordIds.Add(Context.TransactionId);
                candidateVotes.ObtainedActiveVotedVotesAmount = candidateVotes.ObtainedActiveVotedVotesAmount.Add(input.Amount);
                candidateVotes.AllObtainedVotedVotesAmount = candidateVotes.AllObtainedVotedVotesAmount.Add(input.Amount);
            }

            State.CandidateVotes[input.CandidatePublicKey] = candidateVotes;

            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = ElectionContractConstants.VoteSymbol,
                To = Context.Sender,
                Amount = input.Amount,
                Memo = "Get VOTEs."
            });

            State.TokenContract.Lock.Send(new LockInput
            {
                From = Context.Sender,
                Symbol = Context.Variables.NativeSymbol,
                LockId = Context.TransactionId,
                Amount = input.Amount,
                To = Context.Self,
                Usage = "Voting for Main Chain Miner Election."
            });

            State.VoteContract.Vote.Send(new VoteInput
            {
                VotingItemId = State.MinerElectionVotingItemId.Value,
                Amount = input.Amount,
                Option = input.CandidatePublicKey,
                Voter = Context.Sender,
                VoteId = Context.TransactionId
            });

            State.ProfitContract.AddWeight.Send(new AddWeightInput
            {
                ProfitId = State.WelfareHash.Value,
                Receiver = Context.Sender,
                Weight = GetVotesWeight(input.Amount, (long)lockDays),
                EndPeriod = GetEndPeriod((long)lockDays)
            });

            return new Empty();
        }

        public override Empty Withdraw(Hash input)
        {
            var votingRecord = State.VoteContract.GetVotingRecord.Call(input);

            var actualLockedTime = GetTimeSpan(Context.CurrentBlockTime, votingRecord.VoteTimestamp.ToDateTime());
            var claimedLockDays = State.LockTimeMap[input];
            Assert(actualLockedTime >= claimedLockDays,
                $"Still need {claimedLockDays - actualLockedTime} days to unlock your token.");

            // Update Voter's Votes information.
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

            var votingItem = State.VoteContract.GetVotingItem.Call(new GetVotingItemInput
            {
                VotingItemId = State.MinerElectionVotingItemId.Value
            });

            State.TokenContract.Unlock.Send(new UnlockInput
            {
                From = votingRecord.Voter,
                Symbol = votingItem.AcceptedCurrency,
                Amount = votingRecord.Amount,
                LockId = input,
                To = votingItem.Sponsor,
                Usage = $"Withdraw votes for {ElectionContractConstants.Topic}"
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

        public override Empty UpdateCandidateInformation(UpdateCandidateInformationInput input)
        {
            if (input.IsEvilNode)
            {
                var publicKeyByte = ByteArrayHelpers.FromHexString(input.PublicKey);
                State.BlackList.Value.Value.Add(ByteString.CopyFrom(publicKeyByte));
                State.ProfitContract.SubWeight.Send(new SubWeightInput
                {
                    ProfitId = State.SubsidyHash.Value,
                    Receiver = Address.FromPublicKey(publicKeyByte)
                });
                Context.LogDebug(() => $"Marked {input.PublicKey.Substring(0, 10)} as an evil node.");
            }

            var candidateInformation = State.CandidateInformationMap[input.PublicKey];
            candidateInformation.ProducedBlocks += input.RecentlyProducedBlocks;
            candidateInformation.MissedTimeSlots += input.RecentlyMissedTimeSlots;
            State.CandidateInformationMap[input.PublicKey] = candidateInformation;
            return new Empty();
        }

        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            return (long) (((double) lockTime / 270 + 2.0 / 3.0) * votesAmount);
        }

        private long GetEndPeriod(long lockTime)
        {
            var treasury = State.ProfitContract.GetProfitItem.Call(State.TreasuryHash.Value);
            return lockTime.Div(int.Parse(Context.Variables.TimeEachTerm)).Add(treasury.CurrentPeriod);
        }

        private long GetTimeSpan(DateTime endTime, DateTime startTime)
        {
            if ((TimeUnit) State.BaseTimeUnit.Value == TimeUnit.Minutes)
            {
                return (long) (endTime - startTime).TotalMinutes;
            }

            return (long) (endTime - startTime).TotalDays;
        }
    }
}