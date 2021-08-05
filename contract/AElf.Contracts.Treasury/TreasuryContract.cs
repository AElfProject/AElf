using System.Collections.Generic;
using System.Linq;
using AElf.Standards.ACS10;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.CSharp.Core;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Treasury
{
    // ReSharper disable InconsistentNaming
    /// <summary>
    /// The Treasury is the largest profit scheme in AElf main chain.
    /// Actually the Treasury is our Dividends Pool.
    /// Income of the Treasury is mining rewards
    /// (AEDPoS Contract will:
    /// 1. transfer ELF tokens to general ledger of Treasury every time we change term (7 days),
    /// the amount of ELF should be based on blocks produced during last term. 1,000,000 * 1250000 ELF,
    /// then release the Treasury;
    /// 2. Release Treasury)
    /// 3 sub profit schemes:
    /// (Mining Reward for Miners) - 3
    /// (Subsidy for Candidates / Backups) - 1
    /// (Welfare for Electors / Voters / Citizens) - 1
    ///
    /// 3 sub profit schemes for Mining Rewards:
    /// (Basic Rewards) - 4
    /// (Welcome Rewards) - 1
    /// (Flexible Rewards) - 1
    ///
    /// 3 incomes:
    /// 1. 20% total supply of elf, from consensus contract
    /// 2. tx fees.
    /// 3. resource consumption of developer's contracts.
    /// </summary>
    public partial class TreasuryContract : TreasuryContractImplContainer.TreasuryContractImplBase
    {
        public override Empty InitialTreasuryContract(Empty input)
        {
            Assert(!State.Initialized.Value, "Already initialized.");

            State.ProfitContract.Value =
                Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);

            // Create profit schemes: `Treasury`, `CitizenWelfare`, `BackupSubsidy`, `MinerReward`,
            // `MinerBasicReward`, `MinerVotesWeightReward`, `ReElectedMinerReward`
            var profitItemNameList = new List<string>
            {
                "Treasury", "MinerReward", "Subsidy", "Welfare", "Basic Reward", "Votes Weight Reward",
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
                    DelayDistributePeriodCount = i == 3 ? 1 : 0,
                    // Subsidy, Votes Weight Reward and Re-Election Reward can remove beneficiary directly (due to replaceable.)
                    CanRemoveBeneficiaryDirectly = new List<int> {2, 5, 6}.Contains(i)
                });
            }

            State.Initialized.Value = true;

            State.SymbolList.Value = new SymbolList
            {
                Value = {Context.Variables.NativeSymbol}
            };

            return new Empty();
        }

        public override Empty InitialMiningRewardProfitItem(Empty input)
        {
            Assert(State.TreasuryHash.Value == null, "Already initialized.");
            var managingSchemeIds = State.ProfitContract.GetManagingSchemeIds.Call(new GetManagingSchemeIdsInput
            {
                Manager = Context.Self
            }).SchemeIds;

            Assert(managingSchemeIds.Count == 7, "Incorrect schemes count.");

            State.TreasuryHash.Value = managingSchemeIds[0];
            State.RewardHash.Value = managingSchemeIds[1];
            State.SubsidyHash.Value = managingSchemeIds[2];
            State.WelfareHash.Value = managingSchemeIds[3];
            State.BasicRewardHash.Value = managingSchemeIds[4];
            State.WelcomeRewardHash.Value = managingSchemeIds[5];
            State.FlexibleRewardHash.Value = managingSchemeIds[6];

            var electionContractAddress =
                Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
            if (electionContractAddress != null)
            {
                State.ProfitContract.ResetManager.Send(new ResetManagerInput
                {
                    SchemeId = managingSchemeIds[2],
                    NewManager = electionContractAddress
                });
                State.ProfitContract.ResetManager.Send(new ResetManagerInput
                {
                    SchemeId = managingSchemeIds[3],
                    NewManager = electionContractAddress
                });
            }

            BuildTreasury();

            var treasuryVirtualAddress = Address.FromPublicKey(State.ProfitContract.Value.Value.Concat(
                managingSchemeIds[0].Value.ToByteArray().ComputeHash()).ToArray());
            State.TreasuryVirtualAddress.Value = treasuryVirtualAddress;

            return new Empty();
        }

        public override Empty Release(ReleaseInput input)
        {
            RequireAEDPoSContractStateSet();
            Assert(
                Context.Sender == State.AEDPoSContract.Value,
                "Only AElf Consensus Contract can release profits from Treasury.");
            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.TreasuryHash.Value,
                Period = input.PeriodNumber,
                AmountsMap = {State.SymbolList.Value.Value.ToDictionary(s => s, s => 0L)}
            });
            RequireElectionContractStateSet();
            var previousTermInformation = State.AEDPoSContract.GetPreviousTermInformation.Call(new Int64Value
            {
                Value = input.PeriodNumber
            });
            
            var victories = State.ElectionContract.GetVictories.Call(new Empty()).Value.Select(bs => bs.ToHex())
                .ToList();
            var newElectedMiners = victories.Where(p => State.LatestMinedTerm[p] == 0).ToList();

            UpdateStateBeforeDistribution(previousTermInformation, newElectedMiners);
            ReleaseTreasurySubProfitItems(input.PeriodNumber);
            UpdateStateAfterDistribution(previousTermInformation, victories);
            return new Empty();
        }

        public override Empty Donate(DonateInput input)
        {
            Assert(input.Amount > 0, "Invalid amount of donating. Amount needs to be greater than 0.");
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            if (!State.TokenContract.IsTokenAvailableForMethodFee.Call(new StringValue {Value = input.Symbol}).Value)
            {
                return new Empty();
            }

            if (State.TokenConverterContract.Value == null)
            {
                State.TokenConverterContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);
            }

            var isNativeSymbol = input.Symbol == Context.Variables.NativeSymbol;
            var canExchangeWithNativeSymbol =
                isNativeSymbol ||
                State.TokenConverterContract.IsSymbolAbleToSell
                    .Call(new StringValue {Value = input.Symbol}).Value;

            if (Context.Sender != Context.Self)
            {
                State.TokenContract.TransferFrom.Send(new TransferFromInput
                {
                    From = Context.Sender,
                    To = Context.Self,
                    Symbol = input.Symbol,
                    Amount = input.Amount,
                    Memo = "Donate to treasury.",
                });
            }

            var needToConvert = !isNativeSymbol && canExchangeWithNativeSymbol;
            if (needToConvert)
            {
                ConvertToNativeToken(input.Symbol, input.Amount);
            }
            else
            {
                State.TokenContract.Approve.Send(new ApproveInput
                {
                    Symbol = input.Symbol,
                    Amount = input.Amount,
                    Spender = State.ProfitContract.Value
                });

                State.ProfitContract.ContributeProfits.Send(new ContributeProfitsInput
                {
                    SchemeId = State.TreasuryHash.Value,
                    Symbol = input.Symbol,
                    Amount = input.Amount
                });

                var donatesOfCurrentBlock = State.DonatedDividends[Context.CurrentHeight];
                if (donatesOfCurrentBlock != null && Context.Variables.NativeSymbol == input.Symbol &&
                    donatesOfCurrentBlock.Value.ContainsKey(Context.Variables.NativeSymbol))
                {
                    donatesOfCurrentBlock.Value[Context.Variables.NativeSymbol] = donatesOfCurrentBlock
                        .Value[Context.Variables.NativeSymbol].Add(input.Amount);
                }
                else
                {
                    donatesOfCurrentBlock = new Dividends
                    {
                        Value =
                        {
                            {input.Symbol, input.Amount}
                        }
                    };
                }

                State.DonatedDividends[Context.CurrentHeight] = donatesOfCurrentBlock;

                Context.Fire(new DonationReceived
                {
                    From = Context.Sender,
                    Symbol = input.Symbol,
                    Amount = input.Amount,
                    PoolContract = Context.Self
                });
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

        public override Empty ChangeTreasuryController(AuthorityInfo input)
        {
            AssertPerformedByTreasuryController();
            Assert(CheckOrganizationExist(input), "Invalid authority input.");
            State.TreasuryController.Value = input;
            return new Empty();
        }

        public override Empty SetSymbolList(SymbolList input)
        {
            AssertPerformedByTreasuryController();
            Assert(input.Value.Contains(Context.Variables.NativeSymbol), "Need to contain native symbol.");
            if (State.TokenContract.Value == null)
            {
                State.TokenContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenContractSystemName);
            }

            if (State.TokenConverterContract.Value == null)
            {
                State.TokenConverterContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);
            }

            foreach (var symbol in input.Value.Where(s => s != Context.Variables.NativeSymbol))
            {
                var isTreasuryInWhiteList = State.TokenContract.IsInWhiteList.Call(new IsInWhiteListInput
                {
                    Symbol = symbol,
                    Address = Context.Self
                }).Value;
                Assert(
                    State.TokenContract.IsTokenAvailableForMethodFee.Call(new StringValue {Value = symbol}).Value ||
                    isTreasuryInWhiteList, "Symbol need to be profitable.");
                Assert(!State.TokenConverterContract.IsSymbolAbleToSell.Call(new StringValue {Value = symbol}).Value,
                    $"Token {symbol} doesn't need to set to symbol list because it would become native token after donation.");
            }

            State.SymbolList.Value = input;
            return new Empty();
        }

        public override Empty SetDividendPoolWeightSetting(DividendPoolWeightSetting input)
        {
            AssertPerformedByTreasuryController();
            Assert(
                input.CitizenWelfareWeight > 0 && input.BackupSubsidyWeight > 0 &&
                input.MinerRewardWeight > 0,
                "invalid input");
            ResetSubSchemeToTreasury(input);
            State.DividendPoolWeightSetting.Value = input;
            return new Empty();
        }

        public override Empty SetMinerRewardWeightSetting(MinerRewardWeightSetting input)
        {
            AssertPerformedByTreasuryController();
            Assert(
                input.BasicMinerRewardWeight > 0 && input.WelcomeRewardWeight > 0 &&
                input.FlexibleRewardWeight > 0,
                "invalid input");
            ResetSubSchemeToMinerReward(input);
            State.MinerRewardWeightSetting.Value = input;
            return new Empty();
        }

        #region Private methods

        private void ConvertToNativeToken(string symbol, long amount)
        {
            State.TokenContract.Approve.Send(new ApproveInput
            {
                Spender = State.TokenConverterContract.Value,
                Symbol = symbol,
                Amount = amount
            });

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

        private void BuildTreasury()
        {
            if (State.DividendPoolWeightSetting.Value == null)
            {
                var dividendPoolWeightSetting = GetDefaultDividendPoolWeightSetting();
                ResetSubSchemeToTreasury(dividendPoolWeightSetting);
                State.DividendPoolWeightSetting.Value = dividendPoolWeightSetting;
            }

            if (State.MinerRewardWeightSetting.Value == null)
            {
                var minerRewardWeightSetting = GetDefaultMinerRewardWeightSetting();
                ResetSubSchemeToMinerReward(minerRewardWeightSetting);
                State.MinerRewardWeightSetting.Value = minerRewardWeightSetting;
            }
        }

        private void ReleaseTreasurySubProfitItems(long termNumber)
        {
            var amountsMap = State.SymbolList.Value.Value.ToDictionary(s => s, s => 0L);
            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.RewardHash.Value,
                Period = termNumber,
                AmountsMap = {amountsMap}
            });

            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.BasicRewardHash.Value,
                Period = termNumber,
                AmountsMap = {amountsMap}
            });

            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.WelcomeRewardHash.Value,
                Period = termNumber,
                AmountsMap = {amountsMap}
            });

            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.FlexibleRewardHash.Value,
                Period = termNumber,
                AmountsMap = {amountsMap}
            });
        }

        private void RequireAEDPoSContractStateSet()
        {
            if (State.AEDPoSContract.Value == null)
            {
                State.AEDPoSContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            }
        }

        private void RequireElectionContractStateSet()
        {
            if (State.ElectionContract.Value == null)
            {
                State.ElectionContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
            }
        }

        private void UpdateStateBeforeDistribution(Round previousTermInformation, List<string> newElectedMiners)
        {
            var previousPreviousTermInformation = State.AEDPoSContract.GetPreviousTermInformation.Call(new Int64Value
            {
                Value = previousTermInformation.TermNumber.Sub(1)
            });

            UpdateBasicMinerRewardWeights(new List<Round> {previousPreviousTermInformation, previousTermInformation});
            UpdateWelcomeRewardWeights(previousTermInformation, newElectedMiners);
            UpdateFlexibleRewardWeights(newElectedMiners);
        }

        private void UpdateStateAfterDistribution(Round previousTermInformation, List<string> victories)
        {
            foreach (var victory in victories)
            {
                State.LatestMinedTerm[victory] = previousTermInformation.TermNumber;
            }
        }

        /// <summary>
        /// Remove current total shares of Basic Reward,
        /// Add new shares for miners of next term.
        /// 1 share for each miner.
        /// </summary>
        /// <param name="previousTermInformation"></param>
        private void UpdateBasicMinerRewardWeights(IReadOnlyCollection<Round> previousTermInformation)
        {
            if (previousTermInformation.First().RealTimeMinersInformation != null)
            {
                State.ProfitContract.RemoveBeneficiaries.Send(new RemoveBeneficiariesInput
                {
                    SchemeId = State.BasicRewardHash.Value,
                    Beneficiaries =
                    {
                        previousTermInformation.First().RealTimeMinersInformation.Keys.Select(k =>
                            Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(k)))
                    }
                });
            }

            var averageProducedBlocksCount = CalculateAverage(previousTermInformation.Last().RealTimeMinersInformation
                .Values
                .Select(i => i.ProducedBlocks).ToList());
            // Manage weights of `MinerBasicReward`
            State.ProfitContract.AddBeneficiaries.Send(new AddBeneficiariesInput
            {
                SchemeId = State.BasicRewardHash.Value,
                EndPeriod = previousTermInformation.Last().TermNumber,
                BeneficiaryShares =
                {
                    previousTermInformation.Last().RealTimeMinersInformation.Values.Select(i =>
                    {
                        var shares = CalculateShares(i.ProducedBlocks, averageProducedBlocksCount);
                        return new BeneficiaryShare
                        {
                            Beneficiary = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(i.Pubkey)),
                            Shares = shares
                        };
                    })
                }
            });
        }

        /// <summary>
        /// Just to make sure not using double type.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        private long CalculateAverage(List<long> list)
        {
            var sum = list.Sum();
            return sum.Div(list.Count);
        }

        private long CalculateShares(long producedBlocksCount, long averageProducedBlocksCount)
        {
            if (producedBlocksCount < averageProducedBlocksCount.Div(5).Mul(4))
            {
                // If count < (4/5) * average_count, then ratio will be (count / average_count)
                return producedBlocksCount.Div(averageProducedBlocksCount).Mul(producedBlocksCount);
            }

            if (producedBlocksCount < averageProducedBlocksCount.Div(2))
            {
                // If count < (1/2) * average_count, then this node won't share Basic Miner Reward.
                return 0;
            }

            return producedBlocksCount;
        }

        private void UpdateWelcomeRewardWeights(Round previousTermInformation, List<string> newElectedMiners)
        {
            var previousMinerAddresses = previousTermInformation.RealTimeMinersInformation.Keys
                .Select(k => Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(k))).ToList();
            var possibleWelcomeBeneficiaries = new RemoveBeneficiariesInput
            {
                SchemeId = State.WelcomeRewardHash.Value,
                Beneficiaries = { previousMinerAddresses }
            };
            State.ProfitContract.RemoveBeneficiaries.Send(possibleWelcomeBeneficiaries);
            State.ProfitContract.RemoveSubScheme.Send(new RemoveSubSchemeInput
            {
                SchemeId = State.WelcomeRewardHash.Value,
                SubSchemeId = State.BasicRewardHash.Value
            });

            if (newElectedMiners.Any())
            {
                var newBeneficiaries = new AddBeneficiariesInput
                {
                    SchemeId = State.WelcomeRewardHash.Value,
                    EndPeriod = previousTermInformation.TermNumber.Add(1)
                };
                foreach (var minerAddress in newElectedMiners.Select(miner =>
                    Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(miner))))
                {
                    newBeneficiaries.BeneficiaryShares.Add(new BeneficiaryShare
                    {
                        Beneficiary = minerAddress,
                        Shares = 1
                    });
                }

                if (newBeneficiaries.BeneficiaryShares.Any())
                {
                    State.ProfitContract.AddBeneficiaries.Send(newBeneficiaries);
                }
            }
            else
            {
                State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
                {
                    SchemeId = State.WelcomeRewardHash.Value,
                    SubSchemeId = State.BasicRewardHash.Value,
                    SubSchemeShares = 1
                });
            }
        }
        
        private void UpdateFlexibleRewardWeights(List<string> newElectedMiners)
        {
            State.ProfitContract.RemoveSubScheme.Send(new RemoveSubSchemeInput
            {
                SchemeId = State.FlexibleRewardHash.Value,
                SubSchemeId = State.WelfareHash.Value
            });
            State.ProfitContract.RemoveSubScheme.Send(new RemoveSubSchemeInput
            {
                SchemeId = State.FlexibleRewardHash.Value,
                SubSchemeId = State.BasicRewardHash.Value
            });

            if (newElectedMiners.Any())
            {
                State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
                {
                    SchemeId = State.WelcomeRewardHash.Value,
                    SubSchemeId = State.WelfareHash.Value,
                    SubSchemeShares = 1
                });
            }
            else
            {
                State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
                {
                    SchemeId = State.WelcomeRewardHash.Value,
                    SubSchemeId = State.BasicRewardHash.Value,
                    SubSchemeShares = 1
                });
            }
        }

        private void AssertPerformedByTreasuryController()
        {
            if (State.TreasuryController.Value == null)
            {
                State.TreasuryController.Value = GetDefaultTreasuryController();
            }

            Assert(Context.Sender == State.TreasuryController.Value.OwnerAddress, "no permission");
        }

        private AuthorityInfo GetDefaultTreasuryController()
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

        #endregion

        public override GetWelfareRewardAmountSampleOutput GetWelfareRewardAmountSample(
            GetWelfareRewardAmountSampleInput input)
        {
            const long sampleAmount = 10000;
            var welfareHash = State.WelfareHash.Value;
            var output = new GetWelfareRewardAmountSampleOutput();
            var welfareScheme = State.ProfitContract.GetScheme.Call(welfareHash);
            var releasedInformation = State.ProfitContract.GetDistributedProfitsInfo.Call(
                new SchemePeriod
                {
                    SchemeId = welfareHash,
                    Period = welfareScheme.CurrentPeriod.Sub(1)
                });
            var totalShares = releasedInformation.TotalShares;
            if (totalShares == 0)
            {
                return new GetWelfareRewardAmountSampleOutput();
            }

            var totalAmount = releasedInformation.AmountsMap;
            foreach (var lockTime in input.Value)
            {
                var shares = GetVotesWeight(sampleAmount, lockTime);
                // In case of arithmetic overflow
                var decimalAmount = (decimal) totalAmount[Context.Variables.NativeSymbol];
                var decimalShares = (decimal) shares;
                var decimalTotalShares = (decimal) totalShares;
                var amount = decimalAmount * decimalShares / decimalTotalShares;
                output.Value.Add((long) amount);
            }

            return output;
        }

        public override Dividends GetUndistributedDividends(Empty input)
        {
            return new Dividends
            {
                Value =
                {
                    State.SymbolList.Value.Value.Select(s => State.TokenContract.GetBalance.Call(new GetBalanceInput
                    {
                        Owner = State.TreasuryVirtualAddress.Value,
                        Symbol = s
                    })).ToDictionary(b => b.Symbol, b => b.Balance)
                }
            };
        }

        public override Hash GetTreasurySchemeId(Empty input)
        {
            return State.TreasuryHash.Value ?? Hash.Empty;
        }

        public override AuthorityInfo GetTreasuryController(Empty input)
        {
            if (State.TreasuryController.Value == null)
            {
                return GetDefaultTreasuryController();
            }

            return State.TreasuryController.Value;
        }

        public override SymbolList GetSymbolList(Empty input)
        {
            return State.SymbolList.Value;
        }

        public override MinerRewardWeightProportion GetMinerRewardWeightProportion(Empty input)
        {
            var weightSetting = State.MinerRewardWeightSetting.Value ?? GetDefaultMinerRewardWeightSetting();
            var weightSum = weightSetting.BasicMinerRewardWeight.Add(weightSetting.WelcomeRewardWeight)
                .Add(weightSetting.FlexibleRewardWeight);
            var weightProportion = new MinerRewardWeightProportion
            {
                BasicMinerRewardProportionInfo = new SchemeProportionInfo
                {
                    SchemeId = State.BasicRewardHash.Value,
                    Proportion = weightSetting.BasicMinerRewardWeight
                        .Mul(TreasuryContractConstants.OneHundredPercent).Div(weightSum)
                },
                WelcomeRewardProportionInfo = new SchemeProportionInfo
                {
                    SchemeId = State.FlexibleRewardHash.Value,
                    Proportion = weightSetting.WelcomeRewardWeight
                        .Mul(TreasuryContractConstants.OneHundredPercent).Div(weightSum)
                }
            };
            weightProportion.FlexibleRewardProportionInfo = new SchemeProportionInfo
            {
                SchemeId = State.WelcomeRewardHash.Value,
                Proportion = TreasuryContractConstants.OneHundredPercent
                    .Sub(weightProportion.BasicMinerRewardProportionInfo.Proportion)
                    .Sub(weightProportion.WelcomeRewardProportionInfo.Proportion)
            };
            return weightProportion;
        }

        public override DividendPoolWeightProportion GetDividendPoolWeightProportion(Empty input)
        {
            var weightSetting = State.DividendPoolWeightSetting.Value ?? GetDefaultDividendPoolWeightSetting();
            var weightSum = weightSetting.BackupSubsidyWeight.Add(weightSetting.CitizenWelfareWeight)
                .Add(weightSetting.MinerRewardWeight);
            var weightProportion = new DividendPoolWeightProportion
            {
                BackupSubsidyProportionInfo = new SchemeProportionInfo
                {
                    SchemeId = State.SubsidyHash.Value,
                    Proportion = weightSetting.BackupSubsidyWeight
                        .Mul(TreasuryContractConstants.OneHundredPercent).Div(weightSum)
                },
                CitizenWelfareProportionInfo = new SchemeProportionInfo
                {
                    SchemeId = State.WelfareHash.Value,
                    Proportion = weightSetting.CitizenWelfareWeight
                        .Mul(TreasuryContractConstants.OneHundredPercent).Div(weightSum)
                }
            };
            weightProportion.MinerRewardProportionInfo = new SchemeProportionInfo
            {
                SchemeId = State.RewardHash.Value,
                Proportion = TreasuryContractConstants.OneHundredPercent
                    .Sub(weightProportion.BackupSubsidyProportionInfo.Proportion)
                    .Sub(weightProportion.CitizenWelfareProportionInfo.Proportion)
            };
            return weightProportion;
        }

        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            RequireElectionContractStateSet();
            var weight = State.ElectionContract.GetCalculateVoteWeight.Call(new VoteInformation
            {
                Amount = votesAmount,
                LockTime = lockTime
            });
            return weight.Value;
        }

        private DividendPoolWeightSetting GetDefaultDividendPoolWeightSetting()
        {
            return new DividendPoolWeightSetting
            {
                CitizenWelfareWeight = 15,
                BackupSubsidyWeight = 1,
                MinerRewardWeight = 4
            };
        }

        private MinerRewardWeightSetting GetDefaultMinerRewardWeightSetting()
        {
            return new MinerRewardWeightSetting
            {
                BasicMinerRewardWeight = 2,
                WelcomeRewardWeight = 1,
                FlexibleRewardWeight = 1
            };
        }

        private void ResetSubSchemeToTreasury(DividendPoolWeightSetting newWeightSetting)
        {
            var oldWeightSetting = State.DividendPoolWeightSetting.Value ?? new DividendPoolWeightSetting();
            var parentSchemeId = State.TreasuryHash.Value;
            // Register or reset `MinerReward` to `Treasury`
            SendToProfitContractToResetWeight(parentSchemeId, State.RewardHash.Value,
                oldWeightSetting.MinerRewardWeight, newWeightSetting.MinerRewardWeight);
            // Register or reset `BackupSubsidy` to `Treasury`
            SendToProfitContractToResetWeight(parentSchemeId, State.SubsidyHash.Value,
                oldWeightSetting.BackupSubsidyWeight, newWeightSetting.BackupSubsidyWeight);
            // Register or reset `CitizenWelfare` to `Treasury`
            SendToProfitContractToResetWeight(parentSchemeId, State.WelfareHash.Value,
                oldWeightSetting.CitizenWelfareWeight, newWeightSetting.CitizenWelfareWeight);
        }

        private void ResetSubSchemeToMinerReward(MinerRewardWeightSetting newWeightSetting)
        {
            var oldWeightSetting = State.MinerRewardWeightSetting.Value ?? new MinerRewardWeightSetting();
            var parentSchemeId = State.RewardHash.Value;
            // Register or reset `MinerBasicReward` to `MinerReward`
            SendToProfitContractToResetWeight(parentSchemeId, State.BasicRewardHash.Value,
                oldWeightSetting.BasicMinerRewardWeight, newWeightSetting.BasicMinerRewardWeight);
            // Register or reset `WelcomeRewardWeight` to `MinerReward`
            SendToProfitContractToResetWeight(parentSchemeId, State.WelcomeRewardHash.Value,
                oldWeightSetting.WelcomeRewardWeight, newWeightSetting.WelcomeRewardWeight);
            // Register or reset `FlexibleRewardWeight` to `MinerReward`
            SendToProfitContractToResetWeight(parentSchemeId, State.FlexibleRewardHash.Value,
                oldWeightSetting.FlexibleRewardWeight, newWeightSetting.FlexibleRewardWeight);
        }

        private void SendToProfitContractToResetWeight(Hash parentSchemeId, Hash subSchemeId, int oldWeight,
            int newWeight)
        {
            if (oldWeight == newWeight)
                return;

            // old weight equals 0 indicates the subScheme has not been registered
            if (oldWeight > 0)
                State.ProfitContract.RemoveSubScheme.Send(new RemoveSubSchemeInput
                {
                    SchemeId = parentSchemeId,
                    SubSchemeId = subSchemeId
                });

            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = parentSchemeId,
                SubSchemeId = subSchemeId,
                SubSchemeShares = newWeight
            });
        }

        public override Empty UpdateMiningReward(Int64Value input)
        {
            Assert(Context.Sender ==
                   Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName),
                "Only consensus contract can update mining reward.");
            State.MiningReward.Value = input.Value;
            return new Empty();
        }

        public override Dividends GetDividends(Int64Value input)
        {
            Assert(Context.CurrentHeight > input.Value, "Cannot query dividends of a future block.");
            var dividends = State.DonatedDividends[input.Value];

            if (dividends != null && dividends.Value.ContainsKey(Context.Variables.NativeSymbol))
            {
                dividends.Value[Context.Variables.NativeSymbol] =
                    dividends.Value[Context.Variables.NativeSymbol].Add(State.MiningReward.Value);
            }
            else
            {
                dividends = new Dividends
                {
                    Value =
                    {
                        {
                            Context.Variables.NativeSymbol, State.MiningReward.Value
                        }
                    }
                };
            }

            return dividends;
        }

        public override Empty RecordMinerReplacement(RecordMinerReplacementInput input)
        {
            Assert(
                Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName) == Context.Sender,
                "Only AEDPoS Contract can record miner replacement.");

            if (State.ProfitContract.Value == null)
            {
                State.ProfitContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ProfitContractSystemName);
            }

            var latestMinedTerm = State.LatestMinedTerm[input.OldPubkey];
            State.LatestMinedTerm[input.NewPubkey] = latestMinedTerm;
            State.LatestMinedTerm.Remove(input.OldPubkey);

            return new Empty();
        }
    }
}