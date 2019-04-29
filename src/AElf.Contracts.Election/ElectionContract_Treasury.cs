using System.Collections.Generic;
using System.Linq;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Vote;

namespace AElf.Contracts.Election
{
    public partial class ElectionContract
    {
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
            State.RewardHash.Value = createdProfitIds[1];
            State.SubsidyHash.Value = createdProfitIds[2];
            State.WelfareHash.Value = createdProfitIds[3];
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
                "Only AElf Consensus Contract can release profits from Treasury.");

            var totalReleasedAmount = input.MinedBlocks.Mul(ElectionContractConsts.ElfTokenPerBlock);

            var releasingPeriodNumber = input.TermNumber - 1;
            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.TreasuryHash.Value,
                Amount = totalReleasedAmount,
                Period = releasingPeriodNumber
            });

            ReleaseTreasurySubProfitItems(releasingPeriodNumber);

            // Update epoch of voting record btw.
            State.VoteContract.UpdateEpochNumber.Send(new UpdateEpochNumberInput
            {
                EpochNumber = input.TermNumber,
                Topic = ElectionContractConsts.Topic
            });

            // Take snapshot.
            var snapshot = new TermSnapshot
            {
                TermNumber = input.TermNumber - 1,
                TotalBlocks = input.MinedBlocks,
                EndRoundNumber = input.RoundNumber
            };
            foreach (var publicKey in State.Candidates.Value.Value)
            {
                var votes = State.Votes[publicKey.ToHex()];
                var validObtainedVotesAmount = 0L;
                if (votes != null)
                {
                    validObtainedVotesAmount = votes.ValidObtainedVotesAmount;
                }

                snapshot.CandidatesVotes.Add(publicKey.ToHex(), validObtainedVotesAmount);
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

            var currentMiners = State.AElfConsensusContract.GetPreviousRoundInformation.Call(new Empty())
                .RealTimeMinersInformation.Keys.ToList();
            var victories = GetVictories(currentMiners);
            var currentMinersAddress = new List<Address>();
            foreach (var publicKey in currentMiners)
            {
                var address = Address.FromPublicKey(ByteArrayHelpers.FromHexString(publicKey));

                currentMinersAddress.Add(address);

                basicRewardProfitAddWeights.Weights.Add(new WeightMap {Receiver = address, Weight = 1});

                var history = State.Histories[publicKey];
                history.Terms.Add(termNumber - 1);

                if (victories.Contains(ByteString.CopyFrom(ByteArrayHelpers.FromHexString(publicKey))))
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

                var votes = State.Votes[publicKey];
                if (votes != null)
                {
                    votesWeightRewardProfitAddWeights.Weights.Add(new WeightMap
                    {
                        Receiver = address, Weight = votes.ValidObtainedVotesAmount
                    });
                }

                State.Histories[publicKey] = history;
            }

            // Manage weights of `MinerBasicReward`
            basicRewardProfitSubWeights.Receivers.AddRange(currentMinersAddress);
            State.ProfitContract.SubWeights.Send(basicRewardProfitSubWeights);
            State.ProfitContract.AddWeights.Send(basicRewardProfitAddWeights);

            // Manage weights of `ReElectedMinerReward`
            reElectionProfitSubWeights.Receivers.AddRange(currentMinersAddress);
            State.ProfitContract.SubWeights.Send(reElectionProfitSubWeights);
            if (!reElectionProfitAddWeights.Weights.Any())
            {
                // Give this part of reward back to Treasury Virtual Address.
                reElectionProfitAddWeights.Weights.Add(new WeightMap
                    {Receiver = Context.ConvertVirtualAddressToContractAddress(State.TreasuryHash.Value), Weight = 1});
            }

            State.ProfitContract.AddWeights.Send(reElectionProfitAddWeights);

            // Manage weights of `MinerVotesWeightReward`
            votesWeightRewardProfitSubWeights.Receivers.AddRange(currentMinersAddress);
            State.ProfitContract.SubWeights.Send(votesWeightRewardProfitSubWeights);
            if (!votesWeightRewardProfitAddWeights.Weights.Any())
            {
                // Give this part of reward back to Treasury Virtual Address.
                votesWeightRewardProfitAddWeights.Weights.Add(new WeightMap
                    {Receiver = Context.ConvertVirtualAddressToContractAddress(State.TreasuryHash.Value), Weight = 1});
            }

            State.ProfitContract.AddWeights.Send(votesWeightRewardProfitAddWeights);
        }

        #endregion
    }
}