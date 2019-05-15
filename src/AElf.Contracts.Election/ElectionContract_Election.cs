using System;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

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
            State.ConsensusContractSystemName.Value = input.ConsensusContractSystemName;
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
            State.Candidates.Value = new PublicKeysList();
            State.MinimumLockTime.Value = input.MinimumLockTime;
            State.MaximumLockTime.Value = input.MaximumLockTime;
            State.Initialized.Value = true;
            State.BlackList.Value = new PublicKeysList();
            return new Empty();
        }

        public override Empty ConfigElectionContract(ConfigElectionContractInput input)
        {
            State.AEDPoSContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.ConsensusContractSystemName.Value);
            Assert(State.AEDPoSContract.Value == Context.Sender, "Only Consensus Contract can call this method.");
            Assert(State.InitialMiners.Value == null, "Initial miners already set.");
            State.InitialMiners.Value = new PublicKeysList
                {Value = {input.MinerList.Select(k => k.ToMappingKey())}};
            foreach (var publicKey in input.MinerList)
            {
                State.CandidateInformationMap[publicKey] = new CandidateInformation {PublicKey = publicKey};
            }
            State.MinersCount.Value = input.MinerList.Count;
            State.TimeEachTerm.Value = input.TimeEachTerm;
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
            State.AEDPoSContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.ConsensusContractSystemName.Value);

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
                !State.AEDPoSContract.GetCurrentMinerList.Call(new Empty()).PublicKeys
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

            var lockSeconds = (input.EndTimestamp - Context.CurrentBlockTime.ToTimestamp()).Seconds;
            Assert(lockSeconds >= State.MinimumLockTime.Value,
                $"Invalid lock time. At least {State.MinimumLockTime.Value.Div(60).Div(60).Div(24)} days");
            Assert(lockSeconds <= State.MaximumLockTime.Value,
                $"Invalid lock time. At most {State.MaximumLockTime.Value.Div(60).Div(60).Div(24)} days");

            State.LockTimeMap[Context.TransactionId] =
                input.EndTimestamp.Seconds - Context.CurrentBlockTime.ToTimestamp().Seconds;

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
                    PublicKey = input.CandidatePublicKey.ToMappingKey(),
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
                Weight = GetVotesWeight(input.Amount, lockSeconds),
                EndPeriod = GetEndPeriod(lockSeconds) + 1
            });

            return new Empty();
        }

        public override Empty Withdraw(Hash input)
        {
            var votingRecord = State.VoteContract.GetVotingRecord.Call(input);

            var actualLockedTime = (Context.CurrentBlockTime.ToTimestamp() - votingRecord.VoteTimestamp).Seconds;
            var claimedLockDays = State.LockTimeMap[input];
            Assert(actualLockedTime >= claimedLockDays,
                $"Still need {claimedLockDays.Sub(actualLockedTime).Div(86400)} days to unlock your token.");

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

            State.TokenContract.Unlock.Send(new UnlockInput
            {
                From = Context.Sender,
                Symbol = Context.Variables.NativeSymbol,
                Amount = votingRecord.Amount,
                LockId = input,
                To = Context.Self,
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

        public override Empty UpdateCandidateInformation(UpdateCandidateInformationInput input)
        {
            var candidateInformation = State.CandidateInformationMap[input.PublicKey];

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
                // TODO: Set to null.
                State.CandidateInformationMap[input.PublicKey] = new CandidateInformation();
                var candidates = State.Candidates.Value;
                candidates.Value.Remove(ByteString.CopyFrom(publicKeyByte));
                State.Candidates.Value = candidates;
                return new Empty();
            }

            candidateInformation.ProducedBlocks += input.RecentlyProducedBlocks;
            candidateInformation.MissedTimeSlots += input.RecentlyMissedTimeSlots;
            State.CandidateInformationMap[input.PublicKey] = candidateInformation;
            return new Empty();
        }

        public override Empty UpdateMinersCount(UpdateMinersCountInput input)
        {
            Assert(State.AEDPoSContract.Value == Context.Sender,
                "Only consensus contract can update miners count.");
            State.MinersCount.Value = input.MinersCount;
            return new Empty();
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
    }
}