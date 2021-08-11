using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.TestBase;
using AElf.Contracts.Treasury;
using AElf.ContractTestKit.AEDPoSExtension;
using AElf.CSharp.Core;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    public partial class EconomicTests : EconomicTestBase
    {
        [Fact]
        public async Task TreasuryCollection_FirstTerm_Test()
        {
            var distributedAmount = await TreasuryDistribution_FirstTerm_Test();

            // First 7 core data centers can profit from backup subsidy
            var firstSevenCoreDataCenters = MissionedECKeyPairs.CoreDataCenterKeyPairs.Take(7).ToList();
            var balancesBefore = firstSevenCoreDataCenters.ToDictionary(k => k, k =>
                AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(k.PublicKey),
                    Symbol = EconomicTestConstants.TokenSymbol
                })).Balance);
            await ClaimProfits(firstSevenCoreDataCenters, _schemes[SchemeType.BackupSubsidy].SchemeId);
            await CheckBalancesAsync(firstSevenCoreDataCenters,
                distributedAmount / 20 / 7 , balancesBefore);
        }

        [IgnoreOnCIFact]
        public async Task TreasuryCollection_SecondTerm_Test()
        {
            var distributedAmountOfFirstTerm = await TreasuryDistribution_FirstTerm_Test();
            var distributionInformationOfSecondTerm = await TreasuryDistribution_SecondTerm_Test();

            // First 7 core data centers can profit from backup subsidy of term 1 and term 2.
            var firstSevenCoreDataCenters = MissionedECKeyPairs.CoreDataCenterKeyPairs.Take(7).ToList();
            {
                var balancesBefore = firstSevenCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstSevenCoreDataCenters, _schemes[SchemeType.BackupSubsidy].SchemeId);
                var subsidyInFirstTerm = distributedAmountOfFirstTerm / 20 / 7;
                var subsidyInformation = distributionInformationOfSecondTerm[SchemeType.BackupSubsidy];
                var subsidyInSecondTerm = subsidyInformation.Amount / subsidyInformation.TotalShares;
                await CheckBalancesAsync(firstSevenCoreDataCenters,
                    subsidyInFirstTerm + subsidyInSecondTerm - EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // First 7 core data centers can profit from miner basic reward because they acted as miners during second term.
            {
                var balancesBefore = firstSevenCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstSevenCoreDataCenters, _schemes[SchemeType.MinerBasicReward].SchemeId);
                var previousRound = ConsensusStub.GetPreviousTermInformation.CallAsync(new Int64Value {Value = 2})
                    .Result;
                var totalBlocks = previousRound.RealTimeMinersInformation.Values.Sum(i => i.ProducedBlocks);
                foreach (var keyPair in firstSevenCoreDataCenters)
                {
                    var shouldIncrease = distributionInformationOfSecondTerm[SchemeType.MinerBasicReward].Amount *
                                         previousRound.RealTimeMinersInformation[keyPair.PublicKey.ToHex()]
                                             .ProducedBlocks / totalBlocks -
                                         EconomicTestConstants.TransactionFeeOfClaimProfit;
                    var amount = await GetBalanceAsync(Address.FromPublicKey(keyPair.PublicKey));
                    amount.ShouldBe(shouldIncrease + balancesBefore[keyPair]);
                }
            }

            // First 10 voters can profit from citizen welfare.
            var firstTenVoters = MissionedECKeyPairs.CitizenKeyPairs.Take(10).ToList();
            {
                var balancesBefore = firstTenVoters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstTenVoters, _schemes[SchemeType.CitizenWelfare].SchemeId);
                var citizenWelfare = distributionInformationOfSecondTerm[SchemeType.CitizenWelfare].Amount / 10;
                await CheckBalancesAsync(firstTenVoters,
                    citizenWelfare - EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }
        }

        [IgnoreOnCIFact]
        public async Task TreasuryCollection_ThirdTerm_Test()
        {
            var distributedAmountOfFirstTerm = await TreasuryDistribution_FirstTerm_Test();
            var distributionInformationOfSecondTerm = await TreasuryDistribution_SecondTerm_Test();
            var distributionInformationOfThirdTerm = await TreasuryDistribution_ThirdTerm_Test();

            var subsidyInformationOfSecondTerm = distributionInformationOfSecondTerm[SchemeType.BackupSubsidy];
            var subsidyInformationOfThirdTerm = distributionInformationOfThirdTerm[SchemeType.BackupSubsidy];

            // First 7 core data centers can profit from backup subsidy of term 1, term 2 and term 3.
            var firstSevenCoreDataCenters = MissionedECKeyPairs.CoreDataCenterKeyPairs.Take(7).ToList();
            {
                var balancesBefore = firstSevenCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstSevenCoreDataCenters, _schemes[SchemeType.BackupSubsidy].SchemeId);
                var subsidyInFirstTerm = distributedAmountOfFirstTerm / 20 / 7;
                var subsidyInSecondTerm =
                    subsidyInformationOfSecondTerm.Amount / subsidyInformationOfSecondTerm.TotalShares;
                var subsidyInThirdTerm =
                    subsidyInformationOfThirdTerm.Amount / subsidyInformationOfThirdTerm.TotalShares;
                await CheckBalancesAsync(firstSevenCoreDataCenters,
                    subsidyInFirstTerm + subsidyInSecondTerm + subsidyInThirdTerm -
                    EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // Last 12 core data centers can profit from backup subsidy of term 2 and term 3.
            var lastTwelveCoreDataCenters = MissionedECKeyPairs.CoreDataCenterKeyPairs.Skip(7).Take(12).ToList();

            {
                var balancesBefore = lastTwelveCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(lastTwelveCoreDataCenters, _schemes[SchemeType.BackupSubsidy].SchemeId);
                var subsidyInSecondTerm =
                    subsidyInformationOfSecondTerm.Amount / subsidyInformationOfSecondTerm.TotalShares;
                var subsidyInThirdTerm =
                    subsidyInformationOfThirdTerm.Amount / subsidyInformationOfThirdTerm.TotalShares;
                await CheckBalancesAsync(lastTwelveCoreDataCenters,
                    subsidyInSecondTerm + subsidyInThirdTerm -
                    EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // First 7 core data centers can profit from miner basic reward of term 2 and term 3.
            {
                var balancesBefore = firstSevenCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstSevenCoreDataCenters, _schemes[SchemeType.MinerBasicReward].SchemeId);

                // Term 2.
                var secondTermInformation = ConsensusStub.GetPreviousTermInformation
                    .CallAsync(new Int64Value { Value = 2 })
                    .Result;
                var producedBlocksOfSecondTerm = secondTermInformation.RealTimeMinersInformation.Values
                    .Select(i => i.ProducedBlocks).ToList();
                var totalProducedBlocksOfSecondTerm = producedBlocksOfSecondTerm.Sum();
                var averageOfSecondTerm = totalProducedBlocksOfSecondTerm.Div(producedBlocksOfSecondTerm.Count);
                var sharesDictOfSecondTerm = new Dictionary<string, long>();
                foreach (var pubkey in secondTermInformation.RealTimeMinersInformation.Keys)
                {
                    var producedBlocks = secondTermInformation.RealTimeMinersInformation[pubkey].ProducedBlocks;
                    sharesDictOfSecondTerm.Add(pubkey, CalculateShares(producedBlocks, averageOfSecondTerm));
                }
                var totalSharesOfSecondTerm = sharesDictOfSecondTerm.Values.Sum();

                // Term 3.
                var thirdTermInformation = ConsensusStub.GetPreviousTermInformation
                    .CallAsync(new Int64Value { Value = 3 })
                    .Result;
                var producedBlocksOfThirdTerm = thirdTermInformation.RealTimeMinersInformation.Values
                    .Select(i => i.ProducedBlocks).ToList();
                var totalProducedBlocksOfThirdTerm = producedBlocksOfThirdTerm.Sum();
                var averageOfThirdTerm = totalProducedBlocksOfThirdTerm.Div(producedBlocksOfThirdTerm.Count);
                var sharesDictOfThirdTerm = new Dictionary<string, long>();
                foreach (var pubkey in thirdTermInformation.RealTimeMinersInformation.Keys)
                {
                    var producedBlocks = thirdTermInformation.RealTimeMinersInformation[pubkey].ProducedBlocks;
                    sharesDictOfThirdTerm.Add(pubkey, CalculateShares(producedBlocks, averageOfThirdTerm));
                }
                var totalSharesOfThirdTerm = sharesDictOfThirdTerm.Values.Sum();
                foreach (var keyPair in firstSevenCoreDataCenters)
                {
                    var shouldIncreaseForSecondTerm =
                        distributionInformationOfSecondTerm[SchemeType.MinerBasicReward].Amount *
                        sharesDictOfSecondTerm[keyPair.PublicKey.ToHex()] / totalSharesOfSecondTerm;
                    var shouldIncreaseForThirdTerm =
                        distributionInformationOfThirdTerm[SchemeType.MinerBasicReward].Amount *
                        sharesDictOfThirdTerm[keyPair.PublicKey.ToHex()] / totalSharesOfThirdTerm;
                    var amount = await GetBalanceAsync(Address.FromPublicKey(keyPair.PublicKey));
                    amount.ShouldBe(shouldIncreaseForSecondTerm + shouldIncreaseForThirdTerm + balancesBefore[keyPair]);
                }
            }

            // Last 12 core data centers can profit from miner basic reward of term 3.
            {
                var balancesBefore = lastTwelveCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(lastTwelveCoreDataCenters, _schemes[SchemeType.MinerBasicReward].SchemeId);
                var thirdTermInformation = ConsensusStub.GetPreviousTermInformation.CallAsync(new Int64Value {Value = 3})
                    .Result;
                var producedBlocksOfThirdTerm = thirdTermInformation.RealTimeMinersInformation.Values
                    .Select(i => i.ProducedBlocks).ToList();
                var totalProducedBlocksOfThirdTerm = producedBlocksOfThirdTerm.Sum();
                var averageOfThirdTerm = totalProducedBlocksOfThirdTerm.Div(producedBlocksOfThirdTerm.Count);
                var sharesDictOfThirdTerm = new Dictionary<string, long>();
                foreach (var pubkey in thirdTermInformation.RealTimeMinersInformation.Keys)
                {
                    var producedBlocks = thirdTermInformation.RealTimeMinersInformation[pubkey].ProducedBlocks;
                    sharesDictOfThirdTerm.Add(pubkey, CalculateShares(producedBlocks, averageOfThirdTerm));
                }
                var totalSharesOfThirdTerm = sharesDictOfThirdTerm.Values.Sum();
                foreach (var keyPair in lastTwelveCoreDataCenters)
                {
                    var shouldIncrease = distributionInformationOfThirdTerm[SchemeType.MinerBasicReward].Amount *
                                         CalculateShares(sharesDictOfThirdTerm[keyPair.PublicKey.ToHex()],
                                             averageOfThirdTerm) / totalSharesOfThirdTerm -
                                         EconomicTestConstants.TransactionFeeOfClaimProfit;
                    var amount = await GetBalanceAsync(Address.FromPublicKey(keyPair.PublicKey));
                    amount.ShouldBe(shouldIncrease + balancesBefore[keyPair]);
                }
            }

            // Last 2 core data centers can profit from welcome reward (of only term 3)
            {
                var lastTwoCoreDataCenters = MissionedECKeyPairs.CoreDataCenterKeyPairs.Skip(22).Take(2).ToList();
                var balancesBefore = lastTwoCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(lastTwoCoreDataCenters, _schemes[SchemeType.WelcomeReward].SchemeId);
                var welcomeRewardInThirdTerm =
                    distributionInformationOfThirdTerm[SchemeType.WelcomeReward].Amount / 10;
                await CheckBalancesAsync(lastTwoCoreDataCenters,
                    welcomeRewardInThirdTerm - EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // First 10 voters can profit from citizen welfare.
            var firstTenVoters = MissionedECKeyPairs.CitizenKeyPairs.Take(10).ToList();
            {
                var balancesBefore = firstTenVoters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                // We limited profiting, thus ClaimProfits need to be called 4 times to profit all.
                await ClaimProfits(firstTenVoters, _schemes[SchemeType.CitizenWelfare].SchemeId);
                await ClaimProfits(firstTenVoters, _schemes[SchemeType.CitizenWelfare].SchemeId);
                await ClaimProfits(firstTenVoters, _schemes[SchemeType.CitizenWelfare].SchemeId);
                await ClaimProfits(firstTenVoters, _schemes[SchemeType.CitizenWelfare].SchemeId);
                var citizenWelfareInSecondTerm =
                    distributionInformationOfSecondTerm[SchemeType.CitizenWelfare].Amount / 10;
                var citizenWelfareInThirdTerm =
                    distributionInformationOfThirdTerm[SchemeType.CitizenWelfare].Amount / 10;
                await CheckBalancesAsync(firstTenVoters,
                    citizenWelfareInSecondTerm + citizenWelfareInThirdTerm -
                    EconomicTestConstants.TransactionFeeOfClaimProfit * 4,
                    balancesBefore);
            }
        }
    }
}