using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Profit;
using AElf.Contracts.Vote;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract
    {
        public override Empty CreateTreasury(Empty input)
        {
            Assert(!State.TreasuryCreated.Value, "Already created.");

            State.ProfitContract.Value = Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);

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

        public override Empty RegisterToTreasury(Empty input)
        {
            Assert(!State.TreasuryRegistered.Value, "Already registered.");

            var createdProfitIds = State.ProfitContract.GetCreatedProfitItems.Call(new GetCreatedProfitItemsInput
            {
                Creator = Context.Self
            }).ProfitIds;

            Assert(createdProfitIds.Count == 7, "Incorrect profit items count.");

            State.TreasuryHash.Value = createdProfitIds[0];
            State.RewardHash.Value = createdProfitIds[1];
            State.SubsidyHash.Value = createdProfitIds[2];
            State.WelfareHash.Value = createdProfitIds[3];
            State.BasicRewardHash.Value = createdProfitIds[4];
            State.VotesWeightRewardHash.Value = createdProfitIds[5];
            State.ReElectionRewardHash.Value = createdProfitIds[6];

            // Fill `Treasury`
            State.ProfitContract.AddProfits.Send(new AddProfitsInput
            {
                ProfitId = State.TreasuryHash.Value,
                Amount = ElectionContractConstants.VotesTotalSupply
            });

            BuildTreasury();

            State.TreasuryRegistered.Value = true;

            return new Empty();
        }

        public override Empty ReleaseTreasuryProfits(ReleaseTreasuryProfitsInput input)
        {
            Assert(Context.Sender == State.AEDPoSContract.Value,
                "Only AElf Consensus Contract can release profits from Treasury.");

            var totalReleasedAmount = input.MinedBlocks.Mul(ElectionContractConstants.ElfTokenPerBlock);

            var releasingPeriodNumber = input.TermNumber - 1;
            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.TreasuryHash.Value,
                Amount = totalReleasedAmount,
                Period = releasingPeriodNumber
            });

            ReleaseTreasurySubProfitItems(releasingPeriodNumber);

            // Update epoch of voting record btw.
            State.VoteContract.TakeSnapshot.Send(new TakeSnapshotInput
            {
                SnapshotNumber = input.TermNumber - 1,
                VotingItemId = State.MinerElectionVotingItemId.Value
            });

            // Take snapshot.
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

            State.Snapshots[input.TermNumber - 1] = snapshot;

            UpdateTreasurySubItemsWeights(input.TermNumber);

            return new Empty();
        }

        #region Private methods

        private void BuildTreasury()
        {
            // Register `CitizenWelfare` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.WelfareHash.Value,
                SubItemWeight = ElectionContractConstants.CitizenWelfareWeight
            });

            // Register `BackupSubsidy` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.SubsidyHash.Value,
                SubItemWeight = ElectionContractConstants.BackupSubsidyWeight
            });

            // Register `MinerReward` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.RewardHash.Value,
                SubItemWeight = ElectionContractConstants.MinerRewardWeight
            });

            // Register `MinerBasicReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.BasicRewardHash.Value,
                SubItemWeight = ElectionContractConstants.BasicMinerRewardWeight
            });

            // Register `MinerVotesWeightReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.VotesWeightRewardHash.Value,
                SubItemWeight = ElectionContractConstants.VotesWeightRewardWeight
            });

            // Register `ReElectionMinerReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.ReElectionRewardHash.Value,
                SubItemWeight = ElectionContractConstants.ReElectionRewardWeight
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

            // Citizen Welfare release should delay one term.
            // Voter voted during term x, can profit after term (x + 1).
            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.WelfareHash.Value,
                Period = termNumber > 1 ? termNumber - 1 : -1,
                TotalWeight = State.CachedWelfareWeight.Value
            });

            State.CachedWelfareWeight.Value =
                State.ProfitContract.GetProfitItem.Call(State.WelfareHash.Value).TotalWeight;
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

            var previousMiners = State.AEDPoSContract.GetPreviousRoundInformation.Call(new Empty())
                .RealTimeMinersInformation.Keys.ToList();
            var victories = GetVictories(previousMiners);
            var previousMinersAddresses = new List<Address>();
            foreach (var publicKey in previousMiners)
            {
                var address = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(publicKey));

                previousMinersAddresses.Add(address);

                var history = State.CandidateInformationMap[publicKey];
                history.Terms.Add(termNumber - 1);

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

                State.CandidateInformationMap[publicKey] = history;
            }

            var treasuryVirtualAddress = Context.ConvertVirtualAddressToContractAddress(State.TreasuryHash.Value);

            // Manage weights of `MinerBasicReward`
            basicRewardProfitSubWeights.Receivers.AddRange(previousMinersAddresses);
            State.ProfitContract.SubWeights.Send(basicRewardProfitSubWeights);
            basicRewardProfitAddWeights.Weights.AddRange(victories.Select(bs => Address.FromPublicKey(bs.ToByteArray()))
                .Select(a => new WeightMap {Receiver = a, Weight = 1}));
            State.ProfitContract.AddWeights.Send(basicRewardProfitAddWeights);

            // Manage weights of `ReElectedMinerReward`
            reElectionProfitSubWeights.Receivers.AddRange(previousMinersAddresses);
            reElectionProfitSubWeights.Receivers.Add(treasuryVirtualAddress);
            State.ProfitContract.SubWeights.Send(reElectionProfitSubWeights);
            if (!reElectionProfitAddWeights.Weights.Any())
            {
                // Give this part of reward back to Treasury Virtual Address.
                reElectionProfitAddWeights.Weights.Add(new WeightMap
                {
                    Receiver = treasuryVirtualAddress,
                    Weight = 1
                });
            }

            State.ProfitContract.AddWeights.Send(reElectionProfitAddWeights);

            // Manage weights of `MinerVotesWeightReward`
            votesWeightRewardProfitSubWeights.Receivers.AddRange(previousMinersAddresses);
            votesWeightRewardProfitSubWeights.Receivers.Add(treasuryVirtualAddress);
            State.ProfitContract.SubWeights.Send(votesWeightRewardProfitSubWeights);
            if (!votesWeightRewardProfitAddWeights.Weights.Any())
            {
                // Give this part of reward back to Treasury Virtual Address.
                votesWeightRewardProfitAddWeights.Weights.Add(new WeightMap
                {
                    Receiver = treasuryVirtualAddress,
                    Weight = 1
                });
            }

            State.ProfitContract.AddWeights.Send(votesWeightRewardProfitAddWeights);
        }

        #endregion
    }
}