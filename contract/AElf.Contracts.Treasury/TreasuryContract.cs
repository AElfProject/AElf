using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Treasury
{
    public class TreasuryContract : TreasuryContractContainer.TreasuryContractBase
    {
        public override Empty InitialTreasuryContract(InitialTreasuryContractInput input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.ProfitContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);

            // Create profit items: `Treasury`, `CitizenWelfare`, `BackupSubsidy`, `MinerReward`,
            // `MinerBasicReward`, `MinerVotesWeightReward`, `ReElectedMinerReward`
            for (var i = 0; i < 7; i++)
            {
                State.ProfitContract.CreateProfitItem.Send(new CreateProfitItemInput
                {
                    IsReleaseAllBalanceEverytimeByDefault = true
                });
            }

            State.Initialized.Value = true;

            return new Empty();
        }

        public override Empty InitialMiningRewardProfitItem(InitialMiningRewardProfitItemInput profitItemInput)
        {
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
            
            State.ProfitContract.SetTreasuryProfitId.Send(createdProfitIds[0]);

            BuildTreasury();

            return new Empty();
        }

        public override Empty Release(ReleaseInput input)
        {
            Assert(Context.Sender == State.AEDPoSContract.Value,
                "Only AElf Consensus Contract can release profits from Treasury.");

            var totalReleasedAmount = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Owner = State.TreasuryVirtualAddress.Value,
                Symbol = Context.Variables.NativeSymbol
            }).Balance;

            var releasingPeriodNumber = input.TermNumber.Sub(1);
            State.ProfitContract.ReleaseProfit.Send(new ReleaseProfitInput
            {
                ProfitId = State.TreasuryHash.Value,
                Amount = totalReleasedAmount,
                Period = releasingPeriodNumber
            });

            ReleaseTreasurySubProfitItems(releasingPeriodNumber);
            UpdateTreasurySubItemsWeights(input.TermNumber);

            return new Empty();
        }

        public override Empty Donate(DonateInput input)
        {
            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = Context.Self,
                Symbol = input.Symbol,
                Amount = input.Amount,
                Memo = "Donate to treasury."
            });

            if (input.Symbol != Context.Variables.NativeSymbol)
            {
                State.TokenConverterContract.Sell.Send(new SellInput
                {
                    Symbol = input.Symbol,
                    Amount = input.Amount
                });
            }

            return new Empty();
        }

        /// <summary>
        /// Help the contract developer to create Smart Token for that contract,
        /// and set corresponding connector.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override Empty Register(RegisterInput input)
        {
            var sender = Context.Sender;
            Assert(State.ContractSymbols[sender] == null,
                $"Token for contract {sender} already created.");

            // Create Smart Token
            State.TokenContract.Create.Send(new CreateInput
            {
                Symbol = input.TokenSymbol,
                TokenName = input.TokenName,
                Decimals = input.Decimals,
                Issuer = Context.Sender,
                IsBurnable = true,
                TotalSupply = input.TotalSupply
            });
            
            // Set bancor connector.
            State.TokenConverterContract.SetConnector.Send(new Connector
            {
                Symbol = input.TokenSymbol,
                Weight = input.ConnectorWeight,
                IsPurchaseEnabled = true,
                IsVirtualBalanceEnabled = true,
                VirtualBalance = 0
            });

            State.ContractSymbols[sender] = input.TokenSymbol;
            
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
                SubItemWeight = TreasuryContractConstants.CitizenWelfareWeight
            });

            // Register `BackupSubsidy` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.SubsidyHash.Value,
                SubItemWeight = TreasuryContractConstants.BackupSubsidyWeight
            });

            // Register `MinerReward` to `Treasury`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.TreasuryHash.Value,
                SubProfitId = State.RewardHash.Value,
                SubItemWeight = TreasuryContractConstants.MinerRewardWeight
            });

            // Register `MinerBasicReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.BasicRewardHash.Value,
                SubItemWeight = TreasuryContractConstants.BasicMinerRewardWeight
            });

            // Register `MinerVotesWeightReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.VotesWeightRewardHash.Value,
                SubItemWeight = TreasuryContractConstants.VotesWeightRewardWeight
            });

            // Register `ReElectionMinerReward` to `MinerReward`
            State.ProfitContract.RegisterSubProfitItem.Send(new RegisterSubProfitItemInput
            {
                ProfitId = State.RewardHash.Value,
                SubProfitId = State.ReElectionRewardHash.Value,
                SubItemWeight = TreasuryContractConstants.ReElectionRewardWeight
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

            var previousMinersAddresses =
                previousMiners.Select(k => Address.FromPublicKey(ByteArrayHelpers.FromHexString(k)));

            var treasuryVirtualAddress = Context.ConvertVirtualAddressToContractAddress(State.TreasuryHash.Value);

            // TODO: Get this from ElectionContract.
            var victories = new List<ByteString>();

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

        public override GetWelfareRewardAmountSampleOutput GetWelfareRewardAmountSample(
            GetWelfareRewardAmountSampleInput input)
        {
            const long amount = 10000;
            var welfareHash = State.WelfareHash.Value;
            var output = new GetWelfareRewardAmountSampleOutput();
            var welfareItem = State.ProfitContract.GetProfitItem.Call(welfareHash);
            var releasedInformation = State.ProfitContract.GetReleasedProfitsInformation.Call(
                new GetReleasedProfitsInformationInput
                {
                    ProfitId = welfareHash,
                    Period = welfareItem.CurrentPeriod.Sub(1)
                });
            var totalWeight = releasedInformation.TotalWeight;
            var totalAmount = releasedInformation.ProfitsAmount;
            foreach (var lockTime in input.Value)
            {
                var weight = GetVotesWeight(amount, lockTime);
                output.Value.Add(totalAmount.Mul(weight).Div(totalWeight));
            }

            return output;
        }

        public override SInt64Value GetCurrentWelfareReward(Empty input)
        {
            return State.ProfitContract.GetProfitAmount.Call(new ProfitInput {ProfitId = State.WelfareHash.Value});
        }

        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            return lockTime.Div(86400).Div(270).Mul(votesAmount).Add(votesAmount.Mul(2).Div(3));
        }
    }
}