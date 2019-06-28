using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    public partial class ElectionContract : ElectionContractContainer.ElectionContractBase
    {
        public override Empty InitialElectionContract(InitialElectionContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");
            State.Candidates.Value = new PublicKeysList();
            State.MinimumLockTime.Value = input.MinimumLockTime;
            State.MaximumLockTime.Value = input.MaximumLockTime;
            State.Initialized.Value = true;
            State.BlackList.Value = new PublicKeysList();
            State.CurrentTermNumber.Value = 1;
            return new Empty();
        }

        public override Empty ConfigElectionContract(ConfigElectionContractInput input)
        {
            State.AEDPoSContract.Value = Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            Assert(State.AEDPoSContract.Value == Context.Sender, "Only Consensus Contract can call this method.");
            Assert(State.InitialMiners.Value == null, "Initial miners already set.");
            State.InitialMiners.Value = new PublicKeysList
                {Value = {input.MinerList.Select(k => k.ToByteString())}};
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

            State.VoteContract.Value = Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName);

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

        public override Empty TakeSnapshot(TakeElectionSnapshotInput input)
        {
            Context.LogDebug(() => "Entered TakeSnapshot.");
            var snapshot = new TermSnapshot
            {
                MinedBlocks = input.MinedBlocks,
                EndRoundNumber = input.RoundNumber
            };
            foreach (var publicKey in State.Candidates.Value.Value)
            {
                var votes = State.CandidateVotes[publicKey.ToHex()];
                var validObtainedVotesAmount = 0L;
                if (votes != null)
                {
                    validObtainedVotesAmount = votes.ObtainedActiveVotedVotesAmount;
                }

                snapshot.ElectionResult.Add(publicKey.ToHex(), validObtainedVotesAmount);
            }

            // Update snapshot of corresponding voting record by the way.
            State.VoteContract.TakeSnapshot.Send(new TakeSnapshotInput
            {
                SnapshotNumber = input.TermNumber,
                VotingItemId = State.MinerElectionVotingItemId.Value
            });


            State.Snapshots[input.TermNumber] = snapshot;
            State.CurrentTermNumber.Value = input.TermNumber.Add(1);

            var previousMiners = State.AEDPoSContract.GetPreviousRoundInformation.Call(new Empty())
                .RealTimeMinersInformation.Keys.ToList();

            var victories = GetVictories(previousMiners);
            var previousMinersAddresses = new List<Address>();
            foreach (var publicKey in previousMiners)
            {
                var address = Address.FromPublicKey(ByteArrayHelpers.FromHexString(publicKey));

                previousMinersAddresses.Add(address);

/*                var history = State.CandidateInformationMap[publicKey];
                history.Terms.Add(input.TermNumber - 1);

                if (victories.Contains(publicKey.ToByteString()))
                {
                    history.ContinualAppointmentCount = history.ContinualAppointmentCount.Add(1);
                    reElectionProfitAddWeights.Weights.Add(new WeightMap
                    {
                        Receiver = address,
                        Weight = history.ContinualAppointmentCount
                    });
                }
                else
                {
                    history.ContinualAppointmentCount = 0;
                }

                var votes = State.CandidateVotes[publicKey];
                if (votes != null)
                {
                    votesWeightRewardProfitAddWeights.Weights.Add(new WeightMap
                    {
                        Receiver = address,
                        Weight = votes.ObtainedActiveVotedVotesAmount
                    });
                }

                State.CandidateInformationMap[publicKey] = history;*/
            }

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
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            if (State.VoteContract.Value == null)
            {
                State.VoteContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.VoteContractSystemName);
            }
            
            if (State.ProfitContract.Value == null)
            {
                State.ProfitContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
            }
            

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

            var lockSeconds = (input.EndTimestamp - Context.CurrentBlockTime).Seconds;
            Assert(lockSeconds >= State.MinimumLockTime.Value,
                $"Invalid lock time. At least {State.MinimumLockTime.Value.Div(60).Div(60).Div(24)} days");
            Assert(lockSeconds <= State.MaximumLockTime.Value,
                $"Invalid lock time. At most {State.MaximumLockTime.Value.Div(60).Div(60).Div(24)} days");

            State.LockTimeMap[Context.TransactionId] =
                input.EndTimestamp.Seconds - Context.CurrentBlockTime.Seconds;

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
                    PublicKey = input.CandidatePublicKey.ToByteString(),
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

            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = ElectionContractConstants.VoteSymbol,
                To = Context.Sender,
                Amount = input.Amount,
                Memo = "Issue VOTEs."
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

            var actualLockedTime = (Context.CurrentBlockTime - votingRecord.VoteTimestamp).Seconds;
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
            Context.LogDebug(() => "Entered UpdateCandidateInformation");
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

            candidateInformation.ProducedBlocks = candidateInformation.ProducedBlocks.Add(input.RecentlyProducedBlocks);
            candidateInformation.MissedTimeSlots = candidateInformation.MissedTimeSlots.Add(input.RecentlyMissedTimeSlots);
            State.CandidateInformationMap[input.PublicKey] = candidateInformation;
            Context.LogDebug(() => "Leaving UpdateCandidateInformation");

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