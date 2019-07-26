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
        [Fact]
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

        [Fact]
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
    }
}