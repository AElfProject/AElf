using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.Profit;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Economic.AEDPoSExtension.Tests
{
    // ReSharper disable once InconsistentNaming
    public class EconomicTests : EconomicTestBase
    {
        [Fact]
        public async Task TreasuryDistributionTest()
        {
            const int minimumBlocksToChangeTerm =
                AEDPoSExtensionConstants.TimeEachTerm / (AEDPoSExtensionConstants.MiningInterval / 1000);
            const int actualBlocks = minimumBlocksToChangeTerm + 10;
            var treasurySchemeId = await TreasuryStub.GetTreasurySchemeId.CallAsync(new Empty());
            var treasuryScheme = await ProfitStub.GetScheme.CallAsync(treasurySchemeId);
            var minedBlocksInFirstRound = 0L;
            for (var i = 0; i < actualBlocks; i++)
            {
                await BlockMiningService.MineBlockAsync();
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                if (round.TermNumber == 2)
                {
                    var previousRound = await ConsensusStub.GetPreviousRoundInformation.CallAsync(new Empty());
                    minedBlocksInFirstRound = previousRound.RealTimeMinersInformation.Values.Sum(m => m.ProducedBlocks);
                }
            }

            // Check term number.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.TermNumber.ShouldBe(2);
            }

            // Check distributed amount.
            {
                var distributedInformation = await ProfitStub.GetDistributedProfitsInfo.CallAsync(new SchemePeriod
                {
                    SchemeId = treasurySchemeId,
                    Period = 1
                });
                distributedInformation.ProfitsAmount["ELF"].ShouldBe(minedBlocksInFirstRound * 1250_0000);
            }
        }
    }
}