using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Types;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    public partial class EconomicTests : EconomicTestBase
    {
        [Fact(Skip = "Skip for saving time.")]
        public async Task TreasuryCollectionTest_FirstTerm()
        {
            var distributedAmount = await TreasuryDistributionTest_FirstTerm();

            // First 5 core data centers can profit from backup subsidy
            var firstFiveCoreDataCenters = MissionedECKeyPairs.CoreDataCenterKeyPairs.Take(5).ToList();
            var balancesBefore = firstFiveCoreDataCenters.ToDictionary(k => k, k =>
                AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(k.PublicKey),
                    Symbol = EconomicTestConstants.TokenSymbol
                })).Balance);
            await ClaimProfits(firstFiveCoreDataCenters, _schemes[SchemeType.BackupSubsidy].SchemeId);
            await CheckBalancesAsync(firstFiveCoreDataCenters,
                distributedAmount / 5 / 5 - EconomicTestConstants.TransactionFeeOfClaimProfit, balancesBefore);
        }

        [Fact(Skip = "Skip for saving time.")]
        public async Task TreasuryCollectionTest_SecondTerm()
        {
            var distributedAmountOfFirstTerm = await TreasuryDistributionTest_FirstTerm();
            var distributionInformationOfSecondTerm = await TreasuryDistributionTest_SecondTerm();

            // First 5 core data centers can profit from backup subsidy of term 1 and term 2.
            var firstFiveCoreDataCenters = MissionedECKeyPairs.CoreDataCenterKeyPairs.Take(5).ToList();
            {
                var balancesBefore = firstFiveCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstFiveCoreDataCenters, _schemes[SchemeType.BackupSubsidy].SchemeId);
                var subsidyInFirstTerm = distributedAmountOfFirstTerm / 5 / 5;
                var subsidyInformation = distributionInformationOfSecondTerm[SchemeType.BackupSubsidy];
                var subsidyInSecondTerm = subsidyInformation.Amount / subsidyInformation.TotalShares;
                await CheckBalancesAsync(firstFiveCoreDataCenters,
                    subsidyInFirstTerm + subsidyInSecondTerm - EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // First 5 core data centers can profit from miner basic reward because they acted as miners during second term.
            {
                var balancesBefore = firstFiveCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstFiveCoreDataCenters, _schemes[SchemeType.MinerBasicReward].SchemeId);
                var basicRewardInSecondTerm =
                    distributionInformationOfSecondTerm[SchemeType.MinerBasicReward].Amount / 9;
                await CheckBalancesAsync(firstFiveCoreDataCenters,
                    basicRewardInSecondTerm - EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // First 5 core data centers can profit from votes weight reward.
            {
                var balancesBefore = firstFiveCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstFiveCoreDataCenters, _schemes[SchemeType.VotesWeightReward].SchemeId);
                var votesWeightReward = distributionInformationOfSecondTerm[SchemeType.VotesWeightReward].Amount / 5;
                await CheckBalancesAsync(firstFiveCoreDataCenters,
                    votesWeightReward - EconomicTestConstants.TransactionFeeOfClaimProfit,
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
                await ClaimProfits(firstTenVoters, _schemes[SchemeType.CitizenWelfare].SchemeId);
                var citizenWelfare = distributionInformationOfSecondTerm[SchemeType.CitizenWelfare].Amount / 10;
                await CheckBalancesAsync(firstTenVoters,
                    citizenWelfare - EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }
        }

        [Fact(Skip = "Skip for saving time.")]
        public async Task TreasuryCollectionTest_ThirdTerm()
        {
            var distributedAmountOfFirstTerm = await TreasuryDistributionTest_FirstTerm();
            var distributionInformationOfSecondTerm = await TreasuryDistributionTest_SecondTerm();
            var distributionInformationOfThirdTerm = await TreasuryDistributionTest_ThirdTerm();

            var subsidyInformationOfSecondTerm = distributionInformationOfSecondTerm[SchemeType.BackupSubsidy];
            var subsidyInformationOfThirdTerm = distributionInformationOfThirdTerm[SchemeType.BackupSubsidy];

            // First 5 core data centers can profit from backup subsidy of term 1, term 2 and term 3.
            var firstFiveCoreDataCenters = MissionedECKeyPairs.CoreDataCenterKeyPairs.Take(5).ToList();
            {
                var balancesBefore = firstFiveCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstFiveCoreDataCenters, _schemes[SchemeType.BackupSubsidy].SchemeId);
                var subsidyInFirstTerm = distributedAmountOfFirstTerm / 5 / 5;
                var subsidyInSecondTerm =
                    subsidyInformationOfSecondTerm.Amount / subsidyInformationOfSecondTerm.TotalShares;
                var subsidyInThirdTerm =
                    subsidyInformationOfThirdTerm.Amount / subsidyInformationOfThirdTerm.TotalShares;
                await CheckBalancesAsync(firstFiveCoreDataCenters,
                    subsidyInFirstTerm + subsidyInSecondTerm + subsidyInThirdTerm -
                    EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // Last 4 core data centers can profit from backup subsidy of term 2 and term 3.
            var lastFourCoreDataCenters = MissionedECKeyPairs.CoreDataCenterKeyPairs.Skip(5).Take(4).ToList();
            {
                var balancesBefore = lastFourCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(lastFourCoreDataCenters, _schemes[SchemeType.BackupSubsidy].SchemeId);
                var subsidyInSecondTerm =
                    subsidyInformationOfSecondTerm.Amount / subsidyInformationOfSecondTerm.TotalShares;
                var subsidyInThirdTerm =
                    subsidyInformationOfThirdTerm.Amount / subsidyInformationOfThirdTerm.TotalShares;
                await CheckBalancesAsync(lastFourCoreDataCenters,
                    subsidyInSecondTerm + subsidyInThirdTerm -
                    EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // First 5 core data centers can profit from miner basic reward of term 2 and term 3.
            {
                var balancesBefore = firstFiveCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstFiveCoreDataCenters, _schemes[SchemeType.MinerBasicReward].SchemeId);
                var basicRewardInSecondTerm =
                    distributionInformationOfSecondTerm[SchemeType.MinerBasicReward].Amount / 9;
                var basicRewardInThirdTerm =
                    distributionInformationOfThirdTerm[SchemeType.MinerBasicReward].Amount / 9;
                await CheckBalancesAsync(firstFiveCoreDataCenters,
                    basicRewardInSecondTerm + basicRewardInThirdTerm -
                    EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // Last 4 core data centers can profit from miner basic reward of term 3.
            {
                var balancesBefore = lastFourCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(lastFourCoreDataCenters, _schemes[SchemeType.MinerBasicReward].SchemeId);
                var basicRewardInThirdTerm =
                    distributionInformationOfThirdTerm[SchemeType.MinerBasicReward].Amount / 9;
                await CheckBalancesAsync(lastFourCoreDataCenters,
                    basicRewardInThirdTerm - EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // First 5 core data centers can profit from votes weight reward.
            {
                var balancesBefore = firstFiveCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(firstFiveCoreDataCenters, _schemes[SchemeType.VotesWeightReward].SchemeId);
                var votesWeightRewardInSecondTerm =
                    distributionInformationOfSecondTerm[SchemeType.VotesWeightReward].Amount / 5;
                var votesWeightRewardInThirdTerm =
                    distributionInformationOfThirdTerm[SchemeType.VotesWeightReward].Amount / 7;
                await CheckBalancesAsync(firstFiveCoreDataCenters,
                    votesWeightRewardInSecondTerm + votesWeightRewardInThirdTerm -
                    EconomicTestConstants.TransactionFeeOfClaimProfit,
                    balancesBefore);
            }

            // Last 4 core data centers can also profit from votes weight reward. (But less.)
            {
                var balancesBefore = lastFourCoreDataCenters.ToDictionary(k => k, k =>
                    AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                    {
                        Owner = Address.FromPublicKey(k.PublicKey),
                        Symbol = EconomicTestConstants.TokenSymbol
                    })).Balance);
                await ClaimProfits(lastFourCoreDataCenters, _schemes[SchemeType.VotesWeightReward].SchemeId);
                var votesWeightRewardInThirdTerm =
                    distributionInformationOfThirdTerm[SchemeType.VotesWeightReward].Amount / 14;
                await CheckBalancesAsync(lastFourCoreDataCenters,
                    votesWeightRewardInThirdTerm - EconomicTestConstants.TransactionFeeOfClaimProfit,
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