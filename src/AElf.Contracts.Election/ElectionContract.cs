using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
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
            State.AElfConsensusContractSystemName.Value = input.AelfConsensusContractSystemName;
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();
            State.Candidates.Value = new PublicKeysList();
            State.Initialized.Value = true;
            return new Empty();
        }

        public override Empty SetInitialMiners(PublicKeysList input)
        {
            Assert(State.InitialMiners.Value == null, "Initial miners already set.");
            State.InitialMiners.Value = new PublicKeysList {Value = {input.Value}};
            foreach (var publicKey in input.Value)
            {
                State.Histories[publicKey.ToHex()] = new CandidateHistory();
            }
            State.MinersCount.Value = input.Value.Count;
            State.AElfConsensusContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.AElfConsensusContractSystemName.Value);
            return new Empty();
        }

        public override Empty RegisterElectionVotingEvent(RegisterElectionVotingEventInput input)
        {
            Assert(!State.VotingEventRegistered.Value, "Already registered.");
            State.BasicContractZero.Value = Context.GetZeroSmartContractAddress();

            State.TokenContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.TokenContractSystemName.Value);
            State.VoteContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.VoteContractSystemName.Value);
            State.AElfConsensusContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.AElfConsensusContractSystemName.Value);

            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = ElectionContractConsts.VoteSymbol,
                TokenName = "Vote token",
                Issuer = Context.Self,
                Decimals = 2,
                IsBurnable = true,
                TotalSupply = ElectionContractConsts.VotesTotalSupply,
                LockWhiteList = {Context.Self}
            });

            State.TokenContract.Issue.Send(new IssueInput
            {
                Symbol = ElectionContractConsts.VoteSymbol,
                Amount = ElectionContractConsts.VotesTotalSupply,
                To = Context.Self,
                Memo = "Power!"
            });

            State.VoteContract.Register.Send(new VotingRegisterInput
            {
                Topic = ElectionContractConsts.Topic,
                Delegated = true,
                AcceptedCurrency = Context.Variables.NativeSymbol,
                ActiveDays = long.MaxValue,
                TotalEpoch = long.MaxValue
            });

            State.VotingEventRegistered.Value = true;
            return new Empty();
        }

        public override Empty CreateTreasury(CreateTreasuryInput input)
        {
            Assert(!State.TreasuryCreated.Value, "Already created.");

            State.ProfitContract.Value =
                State.BasicContractZero.GetContractAddressByName.Call(State.ProfitContractSystemName.Value);

            // Create profit items: `Treasury`, `CitizenWelfare`, `BackupSubsidy`, `MinerReward`,
            // `MinerBasicReward`, `MinerVotesWeightReward`, `ReElectedMinerReward`
            for (var i = 0; i < 7; i++)
            {
                State.ProfitContract.CreateProfitItem.Send(new CreateProfitItemInput
                {
                    TokenSymbol = Context.Variables.NativeSymbol,
                    ReleaseAllIfAmountIsZero = i != 0
                });
            }

            State.TreasuryCreated.Value = true;

            return new Empty();
        }

        public override Empty RegisterToTreasury(RegisterToTreasuryInput input)
        {
            Assert(!State.TreasuryRegistered.Value, "Already created.");

            var createdProfitIds = State.ProfitContract.GetCreatedProfitItems.Call(new GetCreatedProfitItemsInput
            {
                Creator = Context.Self
            }).ProfitIds;

            Assert(createdProfitIds.Count == 7, "Incorrect profit items count.");

            State.TreasuryHash.Value = createdProfitIds[0];
            State.WelfareHash.Value = createdProfitIds[1];
            State.SubsidyHash.Value = createdProfitIds[2];
            State.RewardHash.Value = createdProfitIds[3];
            State.BasicRewardHash.Value = createdProfitIds[4];
            State.VotesWeightRewardHash.Value = createdProfitIds[5];
            State.ReElectionRewardHash.Value = createdProfitIds[6];

            // Add profits to `Treasury`
            State.ProfitContract.AddProfits.Send(new AddProfitsInput
            {
                ProfitId = State.TreasuryHash.Value,
                Amount = ElectionContractConsts.VotesTotalSupply
            });

            BuildTreasury();

            State.TreasuryRegistered.Value = true;

            return new Empty();
        }

        public override Empty ReleaseTreasuryProfits(ReleaseTreasuryProfitsInput input)
        {
            Assert(Context.Sender == State.AElfConsensusContract.Value,
                "Only AElf Consensus Contract can release profits.");
            
            var totalReleasedAmount = input.MinedBlocks.Mul(ElectionContractConsts.ElfTokenPerBlock);

            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.TreasuryHash.Value,
                Amount = totalReleasedAmount,
                Period = input.TermNumber
            });

            ReleaseTreasurySubProfitItems(input.TermNumber - 1);

            // Update epoch of voting record btw.
            State.VoteContract.UpdateEpochNumber.Send(new UpdateEpochNumberInput
            {
                EpochNumber = input.TermNumber,
                Topic = ElectionContractConsts.Topic
            });

            // Take snapshot.
            var snapshot = new TermSnapshot
            {
                TermNumber = input.TermNumber,
                TotalBlocks = input.MinedBlocks,
                EndRoundNumber = input.RoundNumber
            };
            foreach (var publicKey in State.Candidates.Value.Value)
            {
                snapshot.CandidatesVotes.Add(publicKey.ToHex(),
                    State.Votes[publicKey.ToHex()].ValidObtainedVotesAmount);
            }

            State.Snapshots[input.TermNumber] = snapshot;

            UpdateTreasurySubItemsWeights(input.TermNumber);

            return new Empty();
        }

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
                State.Votes[publicKey] == null || State.Votes[publicKey].ActiveVotesIds == null ||
                State.Votes[publicKey].ActiveVotesIds.Count == 0, "Voter can't announce election.");

            // Add this alias to history information of this candidate.
            var candidateHistory = State.Histories[publicKey];

            if (candidateHistory != null)
            {
                Assert(candidateHistory.State != CandidateState.IsEvilNode,
                    "This candidate already marked as evil node before.");
                Assert(candidateHistory.State == CandidateState.NotAnnounced &&
                       !State.Candidates.Value.Value.Contains(publicKeyByteString),
                    "This public key already announced election.");
                candidateHistory.AnnouncementTransactionId = Context.TransactionId;
                State.Histories[publicKey] = candidateHistory;
            }
            else
            {
                State.Histories[publicKey] = new CandidateHistory
                {
                    AnnouncementTransactionId = Context.TransactionId,
                    State = CandidateState.IsCandidate
                };
            }

            State.Candidates.Value.Value.Add(publicKeyByteString);

            State.TokenContract.Lock.Send(new LockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = Context.Variables.NativeSymbol,
                Amount = ElectionContractConsts.LockTokenForElection,
                LockId = Context.TransactionId,
                Usage = "Lock for announcing election."
            });

            State.VoteContract.AddOption.Send(new AddOptionInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = Context.Self,
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
                State.AElfConsensusContract.GetCurrentMiners.Call(new Empty()).PublicKeys.Contains(publicKeyByteString),
                "Current miners cannot quit election.");

            var history = State.Histories[publicKey];

            State.Candidates.Value.Value.Remove(publicKeyByteString);
            State.TokenContract.Unlock.Send(new UnlockInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = Context.Variables.NativeSymbol,
                LockId = history.AnnouncementTransactionId,
                Amount = ElectionContractConsts.LockTokenForElection,
                Usage = "Quit election."
            });

            State.VoteContract.RemoveOption.Send(new RemoveOptionInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = Context.Self,
                Option = publicKey
            });

            history.State = CandidateState.NotAnnounced;
            history.AnnouncementTransactionId = Hash.Empty;
            State.Histories[publicKey] = history;

            State.ProfitContract.SubWeight.Send(new SubWeightInput
            {
                ProfitId = State.SubsidyHash.Value,
                Receiver = Context.Sender
            });

            return new Empty();
        }

        public override Empty Vote(VoteMinerInput input)
        {
            var lockTime = input.LockTimeUnit == LockTimeUnit.Days ? input.LockTime : input.LockTime * 30;
            Assert(lockTime >= 90, "Should lock token for at least 90 days.");
            State.LockTimeMap[Context.TransactionId] = lockTime;

            // Update Voter's Votes information.
            var voterPublicKeyBytes = Context.RecoverPublicKey();
            var voterPublicKey = voterPublicKeyBytes.ToHex();
            var voterPublicKeyByteString = ByteString.CopyFrom(voterPublicKeyBytes);
            var voterVotes = State.Votes[voterPublicKey];
            if (voterVotes == null)
            {
                voterVotes = new Votes
                {
                    PublicKey = voterPublicKeyByteString,
                    ActiveVotesIds = {Context.TransactionId},
                    ValidVotedVotesAmount = input.Amount,
                    AllVotedVotesAmount = input.Amount
                };
            }
            else
            {
                voterVotes.ActiveVotesIds.Add(Context.TransactionId);
                voterVotes.ValidVotedVotesAmount += input.Amount;
                voterVotes.AllVotedVotesAmount += input.Amount;
            }

            State.Votes[voterPublicKey] = voterVotes;

            // Update Candidate's Votes information.
            var candidateVotes = State.Votes[input.CandidatePublicKey];
            if (candidateVotes == null)
            {
                candidateVotes = new Votes
                {
                    PublicKey = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.CandidatePublicKey)),
                    ObtainedActiveVotesIds = {Context.TransactionId},
                    ValidObtainedVotesAmount = input.Amount,
                    AllObtainedVotesAmount = input.Amount
                };
            }
            else
            {
                candidateVotes.ObtainedActiveVotesIds.Add(Context.TransactionId);
                candidateVotes.ValidObtainedVotesAmount += input.Amount;
                candidateVotes.AllObtainedVotesAmount += input.Amount;
            }

            State.Votes[input.CandidatePublicKey] = candidateVotes;

            State.TokenContract.Transfer.Send(new TransferInput
            {
                Symbol = ElectionContractConsts.VoteSymbol,
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
                Usage = "Voting for Mainchain Election."
            });

            State.VoteContract.Vote.Send(new VoteInput
            {
                Topic = ElectionContractConsts.Topic,
                Sponsor = Context.Self,
                Amount = input.Amount,
                Option = input.CandidatePublicKey,
                Voter = Context.Sender,
                VoteId = Context.TransactionId
            });

            State.ProfitContract.AddWeight.Send(new AddWeightInput
            {
                ProfitId = State.WelfareHash.Value,
                Receiver = Context.Sender,
                Weight = GetVotesWeight(input.Amount, lockTime),
                EndPeriod = GetEndPeriod(lockTime)
            });

            return new Empty();
        }

        public override Empty Withdraw(Hash input)
        {
            var votingRecord = State.VoteContract.GetVotingRecord.Call(input);

            var actualLockedDays = (Context.CurrentBlockTime - votingRecord.VoteTimestamp.ToDateTime()).TotalDays;
            var claimedLockDays = State.LockTimeMap[input];
            Assert(actualLockedDays >= claimedLockDays,
                $"Still need {claimedLockDays - actualLockedDays} days to unlock your token.");

            var voteId = Context.TransactionId;
            // Update Voter's Votes information.
            var voterPublicKey = Context.RecoverPublicKey().ToHex();
            var voterVotes = State.Votes[voterPublicKey];
            voterVotes.ActiveVotesIds.Remove(voteId);
            voterVotes.WithdrawnVotesIds.Add(voteId);
            voterVotes.ValidVotedVotesAmount -= votingRecord.Amount;
            State.Votes[voterPublicKey] = voterVotes;

            // Update Candidate's Votes information.
            var candidateVotes = State.Votes[votingRecord.Option];
            candidateVotes.ObtainedActiveVotesIds.Remove(voteId);
            candidateVotes.ObtainedWithdrawnVotesIds.Add(voteId);
            candidateVotes.ValidObtainedVotesAmount -= votingRecord.Amount;
            State.Votes[votingRecord.Option] = candidateVotes;

            State.TokenContract.Unlock.Send(new UnlockInput
            {
                From = votingRecord.Voter,
                Symbol = votingRecord.Currency,
                Amount = votingRecord.Amount,
                LockId = input,
                To = votingRecord.Sponsor,
                Usage = $"Withdraw votes for {ElectionContractConsts.Topic}"
            });
            
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Sender,
                Amount = votingRecord.Amount,
                Symbol = ElectionContractConsts.VoteSymbol,
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

        public override ElectionResult GetElectionResult(GetElectionResultInput input)
        {
            var votingResult = State.VoteContract.GetVotingResult.Call(new GetVotingResultInput
            {
                Topic = ElectionContractConsts.Topic,
                EpochNumber = input.TermNumber,
                Sponsor = Context.Self
            });

            var result = new ElectionResult
            {
                TermNumber = input.TermNumber,
                IsActive = input.TermNumber == State.CurrentTermNumber.Value,
                Results = {votingResult.Results}
            };

            return result;
        }

        public override Empty UpdateCandidateInformation(UpdateCandidateInformationInput input)
        {
            var history = State.Histories[input.PublicKey];
            history.ProducedBlocks += input.RecentlyProducedBlocks;
            history.MissedTimeSlots += input.RecentlyMissedTimeSlots;
            history.IsEvilNode = input.IsEvilNode;
            State.Histories[input.PublicKey] = history;
            return new Empty();
        }

        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            return (long) (((double) lockTime / 270 + 2.0 / 3.0) * votesAmount);
        }

        private long GetEndPeriod(long lockTime)
        {
            var treasury = State.ProfitContract.GetProfitItem.Call(State.TreasuryHash.Value);
            return lockTime.Div(int.Parse(Context.Variables.DaysEachTerm)).Add(treasury.CurrentPeriod);
        }

        private void BuildTreasury()
        {
            // Register `CitizenWelfare` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.WelfareHash.Value,
                SubItemWeight = ElectionContractConsts.CitizenWelfareWeight
            });

            // Register `BackupSubsidy` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.SubsidyHash.Value,
                SubItemWeight = ElectionContractConsts.BackupSubsidyWeight
            });

            // Register `MinerReward` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.RewardHash.Value,
                SubItemWeight = ElectionContractConsts.MinerRewardWeight
            });

            // Register `MinerBasicReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.BasicRewardHash.Value,
                SubItemWeight = ElectionContractConsts.BasicMinerRewardWeight
            });

            // Register `MinerVotesWeightReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.VotesWeightRewardHash.Value,
                SubItemWeight = ElectionContractConsts.VotesWeightRewardWeight
            });

            // Register `ReElectionMinerReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.ReElectionRewardHash.Value,
                SubItemWeight = ElectionContractConsts.ReElectionRewardWeight
            });
        }

        private void ReleaseTreasurySubProfitItems(long termNumber)
        {
            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.RewardHash.Value,
                Period = termNumber
            });

            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.SubsidyHash.Value,
                Period = termNumber
            });

            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.WelfareHash.Value,
                Period = termNumber
            });

            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.BasicRewardHash.Value,
                Period = termNumber
            });

            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.VotesWeightRewardHash.Value,
                Period = termNumber
            });

            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.ReElectionRewardHash.Value,
                Period = termNumber
            });
        }

        private void UpdateTreasurySubItemsWeights(long termNumber)
        {
            var reElectionProfitAddWeights = new AddWeightsInput
            {
                ProfitId = State.ReElectionRewardHash.Value,
                EndPeriod = termNumber
            };

            var reElectionProfitSubWeights = new SubWeightsInput
            {
                ProfitId = State.ReElectionRewardHash.Value
            };
            
            var basicRewardProfitAddWeights = new AddWeightsInput
            {
                ProfitId = State.BasicRewardHash.Value,
                EndPeriod = termNumber
            };
            
            var basicRewardProfitSubWeights = new SubWeightsInput
            {
                ProfitId = State.BasicRewardHash.Value
            };
            
            var votesWeightRewardProfitAddWeights = new AddWeightsInput
            {
                ProfitId = State.VotesWeightRewardHash.Value,
                EndPeriod = termNumber
            };
            
            var votesWeightRewardProfitSubWeights = new SubWeightsInput
            {
                ProfitId = State.BasicRewardHash.Value
            };

            var victories = GetVictories(new Empty()).Value;
            var currentMiners = State.AElfConsensusContract.GetCurrentMiners.Call(new Empty());
            var currentMinersAddress = new List<Address>();
            foreach (var publicKey in currentMiners.PublicKeys)
            {
                var address = Address.FromPublicKey(publicKey.ToByteArray());
                
                currentMinersAddress.Add(address);
                
                basicRewardProfitAddWeights.Weights.Add(new WeightMap {Receiver = address, Weight = 1});

                var history = State.Histories[publicKey.ToHex()];
                history.Terms.Add(termNumber);

                if (victories.Contains(publicKey))
                {
                    history.ContinualAppointmentCount += 1;
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

                var votes = State.Votes[publicKey.ToHex()];
                if (votes != null)
                {
                    votesWeightRewardProfitAddWeights.Weights.Add(new WeightMap
                    {
                        Receiver = address, Weight = votes.ValidObtainedVotesAmount
                    });
                }

                State.Histories[publicKey.ToHex()] = history;
            }
            
            // Manage weights of `MinerBasicReward`
            basicRewardProfitSubWeights.Receivers.AddRange(currentMinersAddress);
            State.ProfitContract.SubWeights.Send(basicRewardProfitSubWeights);
            State.ProfitContract.AddWeights.Send(basicRewardProfitAddWeights);

            // Manage weights of `ReElectedMinerReward`
            reElectionProfitSubWeights.Receivers.AddRange(currentMinersAddress);
            State.ProfitContract.SubWeights.Send(reElectionProfitSubWeights);
            State.ProfitContract.AddWeights.Send(reElectionProfitAddWeights);

            // Manage weights of `MinerVotesWeightReward`
            if (votesWeightRewardProfitAddWeights.Weights.Any())
            {
                votesWeightRewardProfitSubWeights.Receivers.AddRange(currentMinersAddress);
                State.ProfitContract.SubWeights.Send(votesWeightRewardProfitSubWeights);

                State.ProfitContract.AddWeights.Send(votesWeightRewardProfitAddWeights);
            }
        }
    }
}