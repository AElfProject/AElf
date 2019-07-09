using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Treasury
{
    /// <summary>
    /// The Treasury is the largest profit item in AElf main chain.
    /// Actually the Treasury is our Dividends Pool.
    /// Income of the Treasury is mining rewards
    /// (AEDPoS Contract will:
    /// 1. transfer ELF tokens to general ledger of Treasury every time we change term (7 days),
    /// the amount of ELF should be based on blocks produced during last term. 1,000,000 * 1250000 ELF,
    /// then release the Treasury;
    /// 2. Release Treasury)
    /// 3 sub profit items:
    /// (Mining Reward for Miners) - 3
    /// (Subsidy for Candidates / Backups) - 1
    /// (Welfare for Electors / Voters / Citizens) - 1
    ///
    /// 3 sub profit items for Mining Rewards:
    /// (Basic Rewards) - 4
    /// (Miner's Votes Shares) - 1
    /// (Re-Election Rewards) - 1
    ///
    /// 3 incomes:
    /// 1. 20% total supply of elf, from consensus contract
    /// 2. tx fees.
    /// 3. resource consumption of developer's contracts.
    /// </summary>
    public class TreasuryContract : TreasuryContractContainer.TreasuryContractBase
    {
        public override Empty InitialTreasuryContract(Empty input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.ProfitContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);

            // Create profit items: `Treasury`, `CitizenWelfare`, `BackupSubsidy`, `MinerReward`,
            // `MinerBasicReward`, `MinerVotesWeightReward`, `ReElectedMinerReward`
            var profitItemNameList = new List<string>
            {
                "Treasury", "MinerReward", "Subsidy", "Welfare", "Basic Reward", "Votes Shares Reward",
                "Re-Election Reward"
            };
            for (var i = 0; i < 7; i++)
            {
                var index = i;
                Context.LogDebug(() => profitItemNameList[index]);
                State.ProfitContract.CreateScheme.Send(new CreateSchemeInput
                {
                    IsReleaseAllBalanceEveryTimeByDefault = true,
                    // Distribution of Citizen Welfare will delay one period.
                    DelayDistributePeriodCount = i == 3 ? 1 : 0
                });
            }

            State.Initialized.Value = true;

            return new Empty();
        }

        public override Empty InitialMiningRewardProfitItem(Empty input)
        {
            var createdSchemeIds = State.ProfitContract.GetCreatedSchemeIds.Call(new GetCreatedSchemeIdsInput
            {
                Creator = Context.Self
            }).SchemeIds;

            Assert(createdSchemeIds.Count == 7, "Incorrect profit items count.");

            State.TreasuryHash.Value = createdSchemeIds[0];
            State.RewardHash.Value = createdSchemeIds[1];
            State.SubsidyHash.Value = createdSchemeIds[2];
            State.WelfareHash.Value = createdSchemeIds[3];
            State.BasicRewardHash.Value = createdSchemeIds[4];
            State.VotesWeightRewardHash.Value = createdSchemeIds[5];
            State.ReElectionRewardHash.Value = createdSchemeIds[6];

            BuildTreasury();

            var treasuryVirtualAddress = Address.FromPublicKey(State.ProfitContract.Value.Value.Concat(
                createdSchemeIds[0].Value.ToByteArray().CalculateHash()).ToArray());
            State.TreasuryVirtualAddress.Value = treasuryVirtualAddress;

            return new Empty();
        }

        public override Empty Release(ReleaseInput input)
        {
            if (State.AEDPoSContract.Value == null)
            {
                State.AEDPoSContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            }

            Assert(
                Context.Sender == State.AEDPoSContract.Value,
                "Only AElf Consensus Contract can release profits from Treasury.");

            var releasingPeriodNumber = input.TermNumber;
            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.TreasuryHash.Value,
                Period = releasingPeriodNumber,
                Symbol = Context.Variables.NativeSymbol
            });

            ReleaseTreasurySubProfitItems(releasingPeriodNumber);
            UpdateTreasurySubItemsWeights(input.TermNumber);

            Context.LogDebug(() => "Leaving Release.");
            return new Empty();
        }

        public override Empty Donate(DonateInput input)
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            var isNativeSymbol = input.Symbol == Context.Variables.NativeSymbol;

            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = isNativeSymbol
                    ? State.TreasuryVirtualAddress.Value
                    : Context.Self,
                Symbol = input.Symbol,
                Amount = input.Amount,
                Memo = "Donate to treasury."
            });

            if (input.Symbol != Context.Variables.NativeSymbol)
            {
                ConvertToNativeToken(input.Symbol, input.Amount);
            }

            return new Empty();
        }

        public override Empty DonateAll(DonateAllInput input)
        {
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            var balance = State.TokenContract.GetBalance.Call(new GetBalanceInput
            {
                Symbol = input.Symbol,
                Owner = Context.Sender
            }).Balance;

            Donate(new DonateInput
            {
                Symbol = input.Symbol,
                Amount = balance
            });

            return new Empty();
        }

        private void ConvertToNativeToken(string symbol, long amount)
        {
            if (State.TokenConverterContract.Value == null)
            {
                State.TokenConverterContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);
            }

            State.TokenConverterContract.Sell.Send(new SellInput
            {
                Symbol = symbol,
                Amount = amount
            });
            Context.SendInline(Context.Self, nameof(DonateAll), new DonateAllInput
            {
                Symbol = Context.Variables.NativeSymbol
            });
        }

        #region Private methods

        private void BuildTreasury()
        {
            // Register `CitizenWelfare` to `Treasury`
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.TreasuryHash.Value,
                SubSchemeId = State.WelfareHash.Value,
                SubSchemeShares = TreasuryContractConstants.CitizenWelfareWeight
            });

            // Register `BackupSubsidy` to `Treasury`
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.TreasuryHash.Value,
                SubSchemeId = State.SubsidyHash.Value,
                SubSchemeShares = TreasuryContractConstants.BackupSubsidyWeight
            });

            // Register `MinerReward` to `Treasury`
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.TreasuryHash.Value,
                SubSchemeId = State.RewardHash.Value,
                SubSchemeShares = TreasuryContractConstants.MinerRewardWeight
            });

            // Register `MinerBasicReward` to `MinerReward`
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.RewardHash.Value,
                SubSchemeId = State.BasicRewardHash.Value,
                SubSchemeShares = TreasuryContractConstants.BasicMinerRewardWeight
            });

            // Register `MinerVotesWeightReward` to `MinerReward`
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.RewardHash.Value,
                SubSchemeId = State.VotesWeightRewardHash.Value,
                SubSchemeShares = TreasuryContractConstants.VotesWeightRewardWeight
            });

            // Register `ReElectionMinerReward` to `MinerReward`
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.RewardHash.Value,
                SubSchemeId = State.ReElectionRewardHash.Value,
                SubSchemeShares = TreasuryContractConstants.ReElectionRewardWeight
            });
        }

        private void ReleaseTreasurySubProfitItems(long termNumber)
        {
            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.RewardHash.Value,
                Period = termNumber,
                Symbol = Context.Variables.NativeSymbol
            });

            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.SubsidyHash.Value,
                Period = termNumber,
                Symbol = Context.Variables.NativeSymbol
            });

            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.BasicRewardHash.Value,
                Period = termNumber,
                Symbol = Context.Variables.NativeSymbol
            });

            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.VotesWeightRewardHash.Value,
                Period = termNumber,
                Symbol = Context.Variables.NativeSymbol
            });

            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.ReElectionRewardHash.Value,
                Period = termNumber,
                Symbol = Context.Variables.NativeSymbol
            });

            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.WelfareHash.Value,
                Period = termNumber,
                Symbol = Context.Variables.NativeSymbol
            });
        }

        private void UpdateTreasurySubItemsWeights(long termNumber)
        {
            var endPeriod = termNumber.Add(1);

            var treasuryVirtualAddress = State.TreasuryVirtualAddress.Value;

            if (State.ElectionContract.Value == null)
            {
                State.ElectionContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
            }

            var victories = State.ElectionContract.GetVictories.Call(new Empty()).Value.Select(bs => bs.ToHex())
                .ToList();

            UpdateBasicMinerRewardWeights(endPeriod, victories);

            UpdateReElectionRewardWeights(endPeriod, treasuryVirtualAddress);

            UpdateVotesWeightRewardWeights(endPeriod, victories, treasuryVirtualAddress);
        }

        private void UpdateReElectionRewardWeights(long endPeriod, Address treasuryVirtualAddress)
        {
            var previousMiners = State.AEDPoSContract.GetPreviousRoundInformation.Call(new Empty())
                .RealTimeMinersInformation.Keys.ToList();

            var reElectionProfitAddWeights = new AddBeneficiariesInput
            {
                SchemeId = State.ReElectionRewardHash.Value,
                EndPeriod = endPeriod
            };
            foreach (var previousMiner in previousMiners)
            {
                var continualAppointmentCount =
                    State.ElectionContract.GetCandidateInformation.Call(new StringInput {Value = previousMiner})
                        .ContinualAppointmentCount;
                var minerAddress = Address.FromPublicKey(ByteArrayHelper.FromHexString(previousMiner));
                reElectionProfitAddWeights.BeneficiaryShares.Add(new BeneficiaryShare
                {
                    Beneficiary = minerAddress,
                    Shares = continualAppointmentCount
                });
            }

            if (!reElectionProfitAddWeights.BeneficiaryShares.Any())
            {
                // Give this part of reward back to Treasury Virtual Address.
                reElectionProfitAddWeights.BeneficiaryShares.Add(new BeneficiaryShare
                {
                    Beneficiary = treasuryVirtualAddress,
                    Shares = 1
                });
            }

            State.ProfitContract.AddBeneficiaries.Send(reElectionProfitAddWeights);
        }

        private void UpdateVotesWeightRewardWeights(long endPeriod, IEnumerable<string> victories,
            Address treasuryVirtualAddress)
        {
            var votesWeightRewardProfitAddWeights = new AddBeneficiariesInput
            {
                SchemeId = State.VotesWeightRewardHash.Value,
                EndPeriod = endPeriod
            };

            foreach (var victory in victories)
            {
                var obtainedVotes =
                    State.ElectionContract.GetCandidateVote.Call(new StringInput {Value = victory})
                        .ObtainedActiveVotedVotesAmount;
                var minerAddress = Address.FromPublicKey(ByteArrayHelper.FromHexString(victory));
                votesWeightRewardProfitAddWeights.BeneficiaryShares.Add(new BeneficiaryShare
                {
                    Beneficiary = minerAddress,
                    Shares = obtainedVotes
                });
            }

            if (!votesWeightRewardProfitAddWeights.BeneficiaryShares.Any())
            {
                // Give this part of reward back to Treasury Virtual Address.
                votesWeightRewardProfitAddWeights.BeneficiaryShares.Add(new BeneficiaryShare
                {
                    Beneficiary = treasuryVirtualAddress,
                    Shares = 1
                });
            }

            State.ProfitContract.AddBeneficiaries.Send(votesWeightRewardProfitAddWeights);
        }

        private void UpdateBasicMinerRewardWeights(long endPeriod, IEnumerable<string> victories)
        {
            var basicRewardProfitAddWeights = new AddBeneficiariesInput
            {
                SchemeId = State.BasicRewardHash.Value,
                EndPeriod = endPeriod
            };
            var newMinerWeights = victories.Select(k => Address.FromPublicKey(k.ToByteString().ToByteArray()))
                .Select(a => new BeneficiaryShare {Beneficiary = a, Shares = 1});
            basicRewardProfitAddWeights.BeneficiaryShares.AddRange(newMinerWeights);
            // Manage weights of `MinerBasicReward`
            State.ProfitContract.AddBeneficiaries.Send(basicRewardProfitAddWeights);
        }

        #endregion

        public override GetWelfareRewardAmountSampleOutput GetWelfareRewardAmountSample(
            GetWelfareRewardAmountSampleInput input)
        {
            const long sampleAmount = 10000;
            var welfareHash = State.WelfareHash.Value;
            var output = new GetWelfareRewardAmountSampleOutput();
            var welfareItem = State.ProfitContract.GetScheme.Call(welfareHash);
            var releasedInformation = State.ProfitContract.GetDistributedProfitsInfo.Call(
                new SchemePeriod
                {
                    SchemeId = welfareHash,
                    Period = welfareItem.CurrentPeriod.Sub(1)
                });
            var TotalShares = releasedInformation.TotalShares;
            var totalAmount = releasedInformation.ProfitsAmount;
            foreach (var lockTime in input.Value)
            {
                var Shares = GetVotesWeight(sampleAmount, lockTime);
                output.Value.Add(totalAmount[Context.Variables.NativeSymbol].Mul(Shares).Div(TotalShares));
            }

            return output;
        }

        public override SInt64Value GetCurrentWelfareReward(Empty input)
        {
            var welfareVirtualAddress = Context.ConvertVirtualAddressToContractAddress(State.WelfareHash.Value);
            return new SInt64Value
            {
                Value = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = welfareVirtualAddress,
                    Symbol = Context.Variables.NativeSymbol
                }).Balance
            };
        }

        public override SInt64Value GetCurrentTreasuryBalance(Empty input)
        {
            return new SInt64Value
            {
                Value = State.TokenContract.GetBalance.Call(new GetBalanceInput
                {
                    Owner = State.TreasuryVirtualAddress.Value,
                    Symbol = Context.Variables.NativeSymbol
                }).Balance
            };
        }

        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            return lockTime.Div(86400).Div(270).Mul(votesAmount).Add(votesAmount.Mul(2).Div(3));
        }
    }
}