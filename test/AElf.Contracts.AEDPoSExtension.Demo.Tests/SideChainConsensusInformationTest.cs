using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.ContractTestKit;
using AElf.Kernel.Consensus;
using AElf.Standards.ACS10;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    public class SideChainConsensusInformationTest : AEDPoSExtensionDemoTestBase
    {
        [Fact]
        public async Task SideChainDividendPoolTest()
        {
            InitialContracts();

            await ConsensusStub.SetSymbolList.SendWithExceptionAsync(new SymbolList());
            await ConsensusStub.GetDividends.CallAsync(new Int64Value {Value = 1});
        }

        [Fact]
        public async Task UpdateInformationFromCrossChainTest()
        {
            SetToSideChain();
            InitialContracts();
            var mockedCrossChain = SampleAccount.Accounts.Last();
            var mockedCrossChainStub =
                GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                    ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                    mockedCrossChain.KeyPair);

            var headerInformation = new AElfConsensusHeaderInformation
            {
                Round = new Round
                {
                    RoundNumber = 2,
                    RealTimeMinersInformation =
                    {
                        {Accounts[0].KeyPair.PublicKey.ToHex(), new MinerInRound()},
                        {Accounts[1].KeyPair.PublicKey.ToHex(), new MinerInRound()},
                        {Accounts[2].KeyPair.PublicKey.ToHex(), new MinerInRound()},
                    }
                }
            };

            await mockedCrossChainStub.UpdateInformationFromCrossChain.SendAsync(new BytesValue
            {
                Value = headerInformation.ToByteString()
            });

            var minerList = await ConsensusStub.GetMainChainCurrentMinerList.CallAsync(new Empty());
            minerList.Pubkeys.Select(m => m.ToHex()).ShouldBe(headerInformation.Round.RealTimeMinersInformation.Keys);
        }
    }
}