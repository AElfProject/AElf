using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Consensus;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
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