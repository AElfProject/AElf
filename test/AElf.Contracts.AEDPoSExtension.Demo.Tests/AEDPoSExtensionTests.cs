using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    // ReSharper disable once InconsistentNaming
    public class AEDPoSExtensionTests : AEDPoSExtensionDemoTestBase
    {
        [Fact]
        public async Task DemoTest()
        {
            // Check round information after initialization.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RoundNumber.ShouldBe(1);
                round.TermNumber.ShouldBe(1);
                round.RealTimeMinersInformation.Count.ShouldBe(AEDPoSExtensionConstants.InitialKeyPairCount);
            }

            // We can use this method process testing.
            // Basically this will produce one block with no transaction.
            await BlockMiningService.MineBlockAsync(new List<Transaction>());
            
            // And this will produce one block with one transaction.
            // This transaction will call Create method of Token Contract.
            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                TokenStub.Create.GetTransaction(new CreateInput
                {
                    Symbol = "ELF",
                    Decimals = 8,
                    TokenName = "Test",
                    Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
                    IsBurnable = true,
                    TotalSupply = 1_000_000_000_00000000
                })
            });
            
            // Check whether previous Create transaction successfully executed.
            {
                var tokenInfo = await TokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = "ELF"});
                tokenInfo.Symbol.ShouldBe("ELF");
            }

            // Next steps will check whether the AEDPoS process is correct.
            // Now 2 miners produced block during first round, so there should be 2 miners' OutValue isn't null.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null).ShouldBe(2);
            }

            await BlockMiningService.MineBlockAsync(new List<Transaction>());

            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null).ShouldBe(3);
            }

            // Currently we have 5 miners, and before this line, 3 miners already produced blocks.
            // 3 more blocks will end current round.
            for (var i = 0; i < 3; i++)
            {
                await BlockMiningService.MineBlockAsync(new List<Transaction>());
            }

            // Check round number.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RoundNumber.ShouldBe(2);
            }
            
            // 6 more blocks will end second round.
            for (var i = 0; i < 6; i++)
            {
                await BlockMiningService.MineBlockAsync(new List<Transaction>());
            }
            
            // Check round number.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RoundNumber.ShouldBe(3);
            }
        }
    }
}