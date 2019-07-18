using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKet.AEDPoSExtension;
using AElf.Contracts.TestKit;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contract.Vote
{
    // ReSharper disable once InconsistentNaming
    public class VoteContractTestBase : AEDPoSExtensionTestBase
    {
        internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);
        
        internal TokenContractContainer.TokenContractStub TokenStub =>
            GetTester<TokenContractContainer.TokenContractStub>(
                ContractAddresses[TokenSmartContractAddressNameProvider.Name],
                SampleECKeyPairs.KeyPairs[0]);

        public readonly Dictionary<Hash, Address> ContractAddresses;

        public VoteContractTestBase()
        {
            ContractAddresses = AsyncHelper.RunSync(() => BlockMiningService.DeploySystemContractsAsync(
                new Dictionary<Hash, byte[]>
                {
                    {VoteSmartContractAddressNameProvider.Name, Codes.Single(c => c.Key.Contains("Vote")).Value},
                    {TokenSmartContractAddressNameProvider.Name, Codes.Single(c => c.Key.Contains("MultiToken")).Value},
                }));
        }

        [Fact]
        public async Task AEDPoSExtensionTestingFrameworkTest()
        {
            // Check round information after initialization.
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RoundNumber.ShouldBe(1);
                round.TermNumber.ShouldBe(1);
                round.RealTimeMinersInformation.Count.ShouldBe(AEDPoSExtensionConstants.InitialKeyPairCount);
            }

            await BlockMiningService.MineBlockAsync(new List<Transaction>());
            
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null).ShouldBe(1);
            }
            
            await BlockMiningService.MineBlockAsync(new List<Transaction>());
            
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null).ShouldBe(2);
            }

            await BlockMiningService.MineBlockAsync(new List<Transaction>
            {
                (await TokenStub.Create.SendAsync(new CreateInput
                {
                    Symbol = "ELF",
                    Decimals = 8,
                    TokenName = "Test",
                    Issuer = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
                    IsBurnable = true,
                    TotalSupply = 1_000_000_000_00000000
                })).Transaction,
            });

            {
                var tokenInfo = await TokenStub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = "ELF"});
                tokenInfo.Symbol.ShouldBe("ELF");
            }

            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RealTimeMinersInformation.Values.Count(m => m.OutValue != null).ShouldBe(3);
            }

            for (int i = 0; i < 10; i++)
            {
                await BlockMiningService.MineBlockAsync(new List<Transaction>());
            }
            
            {
                var round = await ConsensusStub.GetCurrentRoundInformation.CallAsync(new Empty());
                round.RoundNumber.ShouldBe(3);
            }

        }
    }
}