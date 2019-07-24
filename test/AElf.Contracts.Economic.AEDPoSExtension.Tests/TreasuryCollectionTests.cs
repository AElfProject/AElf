using System.Threading.Tasks;
using AElf.Contracts.TestKet.AEDPoSExtension;
using Xunit;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    public partial class EconomicTests : EconomicTestBase
    {
        [Fact]
        public async Task TreasuryCollectionTest_FirstTerm()
        {
            await TreasuryDistributionTest_FirstTerm();
            
            // Initial miners can't profit from backup subsidy
            await ClaimProfits(MissionedECKeyPairs.InitialKeyPairs, _schemes[SchemeType.BackupSubsidy].SchemeId);
            await CheckBalancesAsync(MissionedECKeyPairs.InitialKeyPairs, 0);
            
            
        }
    }
}