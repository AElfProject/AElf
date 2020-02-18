using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Profit;
using AElf.Contracts.TokenConverter;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Treasury
{
    // ReSharper disable InconsistentNaming
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
    public partial class TreasuryContract : TreasuryContractContainer.TreasuryContractBase
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
                });
            }

            InitializeVoteWeightInterest();
            State.Initialized.Value = true;

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
            State.VotesWeightRewardHash.Value = managingSchemeIds[5];
            State.ReElectionRewardHash.Value = managingSchemeIds[6];

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
            MaybeLoadAEDPoSContractAddress();
            Assert(
                Context.Sender == State.AEDPoSContract.Value,
                "Only AElf Consensus Contract can release profits from Treasury.");
            State.ProfitContract.DistributeProfits.Send(new DistributeProfitsInput
            {
                SchemeId = State.TreasuryHash.Value,
                Period = input.TermNumber,
                Symbol = Context.Variables.NativeSymbol
            });
            MaybeLoadElectionContractAddress();
            var previousTermInformation = State.AEDPoSContract.GetPreviousTermInformation.Call(new SInt64Value
            {
                Value = input.TermNumber
            });
            UpdateTreasurySubItemsSharesBeforeDistribution(previousTermInformation);
            ReleaseTreasurySubProfitItems(input.TermNumber);
            UpdateTreasurySubItemsSharesAfterDistribution(previousTermInformation);
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

            if (State.TokenConverterContract.Value == null)
            {
                State.TokenConverterContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.TokenConverterContractSystemName);
            }

            var isNativeSymbol = input.Symbol == Context.Variables.NativeSymbol;
            var connector = State.TokenConverterContract.GetPairConnector.Call(new TokenSymbol {Symbol = input.Symbol});
            var canExchangeWithNativeSymbol = connector.DepositConnector != null;

            State.TokenContract.TransferFrom.Send(new TransferFromInput
            {
                From = Context.Sender,
                To = isNativeSymbol || !canExchangeWithNativeSymbol
                    ? State.TreasuryVirtualAddress.Value
                    : Context.Self,
                Symbol = input.Symbol,
                Amount = input.Amount,
                Memo = "Donate to treasury.",
            });

            Context.Fire(new DonationReceived
            {
                From = Context.Sender,
                To = isNativeSymbol || !canExchangeWithNativeSymbol
                    ? State.TreasuryVirtualAddress.Value
                    : Context.Self,
                Symbol = input.Symbol,
                Amount = input.Amount,
                Memo = "Donate to treasury."
            });

            if (input.Symbol != Context.Variables.NativeSymbol && canExchangeWithNativeSymbol)
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
        
        public override Empty SetControllerForManageVoteWeightInterest(Address input)
        {
            AssertControllerForManageVoteWeightInterestSetting();
            Assert(input != null, "invalid input");
            var isNewControllerIsExist = State.ParliamentContract.ValidateOrganizationExist.Call(input);
            Assert(isNewControllerIsExist.Value, "new controller does not exist");
            State.ControllerForManageVoteWeightInterest.Value = input;
            return new Empty();
        }
        
        public override Empty SetVoteWeightInterest(VoteWeightInterestList input)
        {
            AssertControllerForManageVoteWeightInterestSetting();
            Assert(input != null && input.VoteWeightInterestInfos.Count > 0, "invalid input");
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

        private void ConvertToNativeToken(string symbol, long amount)
        {
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
            // Register `MinerReward` to `Treasury`
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.TreasuryHash.Value,
                SubSchemeId = State.RewardHash.Value,
                SubSchemeShares = TreasuryContractConstants.MinerRewardWeight
            });

            // Register `BackupSubsidy` to `Treasury`
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.TreasuryHash.Value,
                SubSchemeId = State.SubsidyHash.Value,
                SubSchemeShares = TreasuryContractConstants.BackupSubsidyWeight
            });

            // Register `CitizenWelfare` to `Treasury`
            State.ProfitContract.AddSubScheme.Send(new AddSubSchemeInput
            {
                SchemeId = State.TreasuryHash.Value,
                SubSchemeId = State.WelfareHash.Value,
                SubSchemeShares = TreasuryContractConstants.CitizenWelfareWeight
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
        }

        private void MaybeLoadAEDPoSContractAddress()
        {
            if (State.AEDPoSContract.Value == null)
            {
                State.AEDPoSContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ConsensusContractSystemName);
            }
        }

        private void MaybeLoadElectionContractAddress()
        {
            if (State.ElectionContract.Value == null)
            {
                State.ElectionContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
            }
        }

        private void UpdateTreasurySubItemsSharesBeforeDistribution(Round previousTermInformation)
        {
            var previousPreviousTermInformation = State.AEDPoSContract.GetPreviousTermInformation.Call(new SInt64Value
            {
                Value = previousTermInformation.TermNumber.Sub(1)
            });

            UpdateBasicMinerRewardWeights(new List<Round> {previousPreviousTermInformation, previousTermInformation});
        }

        private void UpdateTreasurySubItemsSharesAfterDistribution(Round previousTermInformation)
        {
            var victories = State.ElectionContract.GetVictories.Call(new Empty()).Value.Select(bs => bs.ToHex())
                .ToList();

            UpdateReElectionRewardWeights(previousTermInformation, victories);
            UpdateVotesWeightRewardWeights(previousTermInformation, victories);
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
                    Beneficiaries = {previousTermInformation.First().RealTimeMinersInformation.Keys.Select(k =>
                        Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(k)))}
                });
            }

            // Manage weights of `MinerBasicReward`
            State.ProfitContract.AddBeneficiaries.Send(new AddBeneficiariesInput
            {
                SchemeId = State.BasicRewardHash.Value,
                EndPeriod = previousTermInformation.Last().TermNumber,
                BeneficiaryShares =
                {
                    previousTermInformation.Last().RealTimeMinersInformation.Values.Select(i => new BeneficiaryShare
                    {
                        Beneficiary = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(i.Pubkey)),
                        Shares = i.ProducedBlocks
                    })
                }
            });
        }

        /// <summary>
        /// Remove current total shares of Re-Election Reward,
        /// Add shares to re-elected miners based on their continual appointment count.
        /// </summary>
        /// <param name="previousTermInformation"></param>
        /// <param name="victories"></param>
        private void UpdateReElectionRewardWeights(Round previousTermInformation, ICollection<string> victories)
        {
            var previousMinerAddresses = previousTermInformation.RealTimeMinersInformation.Keys
                .Select(k => Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(k))).ToList();
            var reElectionRewardProfitSubBeneficiaries = new RemoveBeneficiariesInput
            {
                SchemeId = State.ReElectionRewardHash.Value,
                Beneficiaries = {previousMinerAddresses}
            };
            State.ProfitContract.RemoveBeneficiaries.Send(reElectionRewardProfitSubBeneficiaries);

            var minerReElectionInformation = State.MinerReElectionInformation.Value ??
                                             InitialMinerReElectionInformation(previousTermInformation.RealTimeMinersInformation.Keys);

            AddBeneficiariesForReElectionScheme(previousTermInformation.TermNumber.Add(1), victories, minerReElectionInformation);

            var recordedMiners = minerReElectionInformation.Clone().ContinualAppointmentTimes.Keys;
            foreach (var miner in recordedMiners)
            {
                if (!victories.Contains(miner))
                {
                    minerReElectionInformation.ContinualAppointmentTimes.Remove(miner);
                }
            }

            State.MinerReElectionInformation.Value = minerReElectionInformation;
        }

        private void AddBeneficiariesForReElectionScheme(long endPeriod, IEnumerable<string> victories,
            MinerReElectionInformation minerReElectionInformation)
        {
            var reElectionProfitAddBeneficiaries = new AddBeneficiariesInput
            {
                SchemeId = State.ReElectionRewardHash.Value,
                EndPeriod = endPeriod
            };

            foreach (var victory in victories)
            {
                if (minerReElectionInformation.ContinualAppointmentTimes.ContainsKey(victory))
                {
                    var minerAddress = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(victory));
                    var continualAppointmentCount =
                        minerReElectionInformation.ContinualAppointmentTimes[victory].Add(1);
                    minerReElectionInformation.ContinualAppointmentTimes[victory] = continualAppointmentCount;
                    reElectionProfitAddBeneficiaries.BeneficiaryShares.Add(new BeneficiaryShare
                    {
                        Beneficiary = minerAddress,
                        Shares = Math.Min(continualAppointmentCount,
                            TreasuryContractConstants.MaximumReElectionRewardShare)
                    });
                }
                else
                {
                    minerReElectionInformation.ContinualAppointmentTimes.Add(victory, 0);
                }
            }

            if (reElectionProfitAddBeneficiaries.BeneficiaryShares.Any())
            {
                State.ProfitContract.AddBeneficiaries.Send(reElectionProfitAddBeneficiaries);
            }
        }

        private MinerReElectionInformation InitialMinerReElectionInformation(ICollection<string> previousMiners)
        {
            var information = new MinerReElectionInformation();
            foreach (var previousMiner in previousMiners)
            {
                information.ContinualAppointmentTimes.Add(previousMiner, 0);
            }

            return information;
        }

        /// <summary>
        /// Remove current total shares of Votes Weight Reward,
        /// Add shares to current miners based on votes they obtained.
        /// </summary>
        /// <param name="previousTermInformation"></param>
        /// <param name="victories"></param>
        private void UpdateVotesWeightRewardWeights(Round previousTermInformation, IEnumerable<string> victories)
        {
            var previousMinerAddresses = previousTermInformation.RealTimeMinersInformation.Keys
                .Select(k => Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(k))).ToList();
            var votesWeightRewardProfitSubBeneficiaries = new RemoveBeneficiariesInput
            {
                SchemeId = State.VotesWeightRewardHash.Value,
                Beneficiaries = {previousMinerAddresses}
            };
            State.ProfitContract.RemoveBeneficiaries.Send(votesWeightRewardProfitSubBeneficiaries);

            var votesWeightRewardProfitAddBeneficiaries = new AddBeneficiariesInput
            {
                SchemeId = State.VotesWeightRewardHash.Value,
                EndPeriod = previousTermInformation.TermNumber.Add(1)
            };

            var dataCenterRankingList = State.ElectionContract.GetDataCenterRankingList.Call(new Empty());

            foreach (var victory in victories)
            {
                var obtainedVotes = 0L;
                if (dataCenterRankingList.DataCenters.ContainsKey(victory))
                {
                    obtainedVotes = dataCenterRankingList.DataCenters[victory];
                }

                var minerAddress = Address.FromPublicKey(ByteArrayHelper.HexStringToByteArray(victory));
                if (obtainedVotes > 0)
                {
                    votesWeightRewardProfitAddBeneficiaries.BeneficiaryShares.Add(new BeneficiaryShare
                    {
                        Beneficiary = minerAddress,
                        Shares = obtainedVotes
                    });
                }
            }

            if (votesWeightRewardProfitAddBeneficiaries.BeneficiaryShares.Any())
            {
                State.ProfitContract.AddBeneficiaries.Send(votesWeightRewardProfitAddBeneficiaries);
            }
        }
        
        private void AssertControllerForManageVoteWeightInterestSetting()
        {
            Assert(Context.Sender == State.ControllerForManageVoteWeightInterest.Value, "no permission");
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

            var totalAmount = releasedInformation.ProfitsAmount;
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

        public override Hash GetTreasurySchemeId(Empty input)
        {
            return State.TreasuryHash.Value ?? Hash.Empty;
        }
        
        public override VoteWeightInterestList GetVoteWeightSetting(Empty input)
        {
            return State.VoteWeightInterestList.Value;
        }
        
        public override Address GetControllerForManageVoteWeightInterest(Empty input)
        {
            return State.ControllerForManageVoteWeightInterest.Value;
        }
        
        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            var lockDays = lockTime.Div(TreasuryContractConstants.DaySec);

            foreach (var instMap in State.VoteWeightInterestList.Value.VoteWeightInterestInfos)
            {
                if (lockDays > instMap.Day)
                    continue;
                var initBase = 1 + (decimal) instMap.Interest / instMap.Capital;
                return ((long) (Pow(initBase, (uint) lockDays) * votesAmount)).Add(votesAmount.Div(2));
            }
            var maxInterestInfo = State.VoteWeightInterestList.Value.VoteWeightInterestInfos.Last();
            var maxInterestBase = 1 + (decimal) maxInterestInfo.Interest / maxInterestInfo.Capital;
            return ((long) (Pow(maxInterestBase, (uint) lockDays) * votesAmount)).Add(votesAmount.Div(2));
        }

        private static decimal Pow(decimal x, uint y)
        {
            if (y == 1)
                return (long) x;
            decimal a = 1m;
            if (y == 0)
                return a;
            var e = new BitArray(BitConverter.GetBytes(y));
            var t = e.Count;
            for (var i = t - 1; i >= 0; --i)
            {
                a *= a;
                if (e[i])
                {
                    a *= x;
                }
            }

            return a;
        }
        
        private void InitializeVoteWeightInterest()
        {
            if (State.VoteWeightInterestList.Value != null)
                return;
            var voteWeightSetting = new VoteWeightInterestList();
            voteWeightSetting.VoteWeightInterestInfos.Add(new VoteWeightInterest
            {
                Day = 365,
                Interest = 1,
                Capital = 1000
            });
            voteWeightSetting.VoteWeightInterestInfos.Add(new VoteWeightInterest
            {
                Day = 730,
                Interest = 15,
                Capital = 10000
            });
            voteWeightSetting.VoteWeightInterestInfos.Add(new VoteWeightInterest
            {
                Day = 1095,
                Interest = 2,
                Capital = 1000
            });
            State.VoteWeightInterestList.Value = voteWeightSetting;
            if (State.ControllerForManageVoteWeightInterest.Value != null) return;
            if (State.ParliamentContract.Value == null)
            {
                State.ParliamentContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ParliamentContractSystemName);
            }
            State.ControllerForManageVoteWeightInterest.Value =
                State.ParliamentContract.GetDefaultOrganizationAddress.Call(new Empty());
        }
    }
}