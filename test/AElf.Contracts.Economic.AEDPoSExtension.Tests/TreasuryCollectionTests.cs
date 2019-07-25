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
            var distributedAmountOfSecondTerm = await TreasuryDistributionTest_SecondTerm();
            
            // First 5 core data centers can profit from miner basic reward because they acted as miners during second term.
            var firstFiveCoreDataCenters = MissionedECKeyPairs.CoreDataCenterKeyPairs.Take(5).ToList();
            var balancesBefore = firstFiveCoreDataCenters.ToDictionary(k => k, k =>
                AsyncHelper.RunSync(() => TokenStub.GetBalance.CallAsync(new GetBalanceInput
                {
                    Owner = Address.FromPublicKey(k.PublicKey),
                    Symbol = EconomicTestConstants.TokenSymbol
                })).Balance);
            await ClaimProfits(firstFiveCoreDataCenters, _schemes[SchemeType.BackupSubsidy].SchemeId);
            await CheckBalancesAsync(firstFiveCoreDataCenters,
                distributedAmountOfSecondTerm / 5 / 9 - EconomicTestConstants.TransactionFeeOfClaimProfit, balancesBefore);
        }
    }
}