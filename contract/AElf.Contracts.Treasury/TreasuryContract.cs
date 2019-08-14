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

            State.Initialized.Value = true;

            return new Empty();
        }

        public override Empty InitialMiningRewardProfitItem(Empty input)
        {
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
            UpdateTreasurySubItemsShares(input.TermNumber);

            Context.LogDebug(() => "Leaving Release.");
            return new Empty();
        }

        public override Empty Donate(DonateInput input)
        {
            Assert(input.Amount > 0, "Invalid amount of donating.");
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

        private void UpdateTreasurySubItemsShares(long termNumber)
        {
            var endPeriod = termNumber.Add(1);

            if (State.ElectionContract.Value == null)
            {
                State.ElectionContract.Value =
                    Context.GetContractAddressByName(SmartContractConstants.ElectionContractSystemName);
            }

            var victories = State.ElectionContract.GetVictories.Call(new Empty()).Value.Select(bs => bs.ToHex())
                .ToList();

            var previousMiners = State.AEDPoSContract.GetPreviousMinerList.Call(new Empty()).Pubkeys.ToList();
            var previousMinerAddress = previousMiners.Select(k => Address.FromPublicKey(k.ToByteArray())).ToList();

            UpdateBasicMinerRewardWeights(endPeriod, victories, previousMinerAddress);

            UpdateReElectionRewardWeights(endPeriod, previousMiners.Select(m => m.ToHex()).ToList(),
                previousMinerAddress, victories);

            UpdateVotesWeightRewardWeights(endPeriod, victories, previousMinerAddress);
        }

        /// <summary>
        /// Remove current total shares of Basic Reward,
        /// Add new shares for miners of next term.
        /// 1 share for each miner.
        /// </summary>
        /// <param name="endPeriod"></param>
        /// <param name="victories"></param>
        /// <param name="previousMinerAddresses"></param>
        private void UpdateBasicMinerRewardWeights(long endPeriod, IEnumerable<string> victories,
            IEnumerable<Address> previousMinerAddresses)
        {
            var basicRewardProfitSubBeneficiaries = new RemoveBeneficiariesInput
            {
                SchemeId = State.BasicRewardHash.Value,
                Beneficiaries = {previousMinerAddresses}
            };
            State.ProfitContract.RemoveBeneficiaries.Send(basicRewardProfitSubBeneficiaries);

            var basicRewardProfitAddBeneficiaries = new AddBeneficiariesInput
            {
                SchemeId = State.BasicRewardHash.Value,
                EndPeriod = endPeriod,
                BeneficiaryShares =
                {
                    victories.Select(k => Address.FromPublicKey(k.ToByteString().ToByteArray()))
                        .Select(a => new BeneficiaryShare {Beneficiary = a, Shares = 1})
                }
            };
            // Manage weights of `MinerBasicReward`
            State.ProfitContract.AddBeneficiaries.Send(basicRewardProfitAddBeneficiaries);
        }

        /// <summary>
        /// Remove current total shares of Re-Election Reward,
        /// Add shares to re-elected miners based on their continual appointment count.
        /// </summary>
        /// <param name="endPeriod"></param>
        /// <param name="previousMiners"></param>
        /// <param name="previousMinerAddresses"></param>
        /// <param name="victories"></param>
        private void UpdateReElectionRewardWeights(long endPeriod, ICollection<string> previousMiners,
            IEnumerable<Address> previousMinerAddresses, ICollection<string> victories)
        {
            var reElectionRewardProfitSubBeneficiaries = new RemoveBeneficiariesInput
            {
                SchemeId = State.ReElectionRewardHash.Value,
                Beneficiaries = {previousMinerAddresses}
            };
            State.ProfitContract.RemoveBeneficiaries.Send(reElectionRewardProfitSubBeneficiaries);

            var minerReElectionInformation = State.MinerReElectionInformation.Value ??
                                             InitialMinerReElectionInformation(previousMiners);

            AddBeneficiariesForReElectionScheme(endPeriod, victories, minerReElectionInformation);

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
                        Shares = continualAppointmentCount
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
        /// <param name="endPeriod"></param>
        /// <param name="victories"></param>
        /// <param name="previousMinerAddresses"></param>
        private void UpdateVotesWeightRewardWeights(long endPeriod, IEnumerable<string> victories,
            IEnumerable<Address> previousMinerAddresses)
        {
            var votesWeightRewardProfitSubBeneficiaries = new RemoveBeneficiariesInput
            {
                SchemeId = State.VotesWeightRewardHash.Value,
                Beneficiaries = {previousMinerAddresses}
            };
            State.ProfitContract.RemoveBeneficiaries.Send(votesWeightRewardProfitSubBeneficiaries);

            var votesWeightRewardProfitAddBeneficiaries = new AddBeneficiariesInput
            {
                SchemeId = State.VotesWeightRewardHash.Value,
                EndPeriod = endPeriod
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
                output.Value.Add(totalAmount[Context.Variables.NativeSymbol].Mul(shares).Div(totalShares));
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

        private long GetVotesWeight(long votesAmount, long lockTime)
        {
            return lockTime.Div(86400).Div(270).Mul(votesAmount).Add(votesAmount.Mul(2).Div(3));
        }
    }
}