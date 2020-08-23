using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs10;
using Acs3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests
{
    public class SideChainConsensusInformationTest : AEDPoSExtensionDemoTestBase
    {
        [Fact]
        public async Task UpdateConsensusInformationTest()
        {
            SetToSideChain();
            InitialContracts();
            InitialAcs3Stubs();
            await ParliamentStubs.First().Initialize.SendAsync(new Parliament.InitializeInput());
            var defaultOrganizationAddress =
                await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
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
            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(ConsensusStub.UpdateConsensusInformation),
                Params = new ConsensusInformation
                {
                    Value = ByteString.CopyFrom(headerInformation.ToByteArray())
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress
            });

            var minerList = await ConsensusStub.GetMainChainCurrentMinerList.CallAsync(new Empty());
            minerList.Pubkeys.Select(m => m.ToHex()).ShouldBe(headerInformation.Round.RealTimeMinersInformation.Keys);
        }

        [Fact]
        public async Task SideChainConsensusInformationControllerTest()
        {
            SetToSideChain();
            InitialContracts();
            InitialAcs3Stubs();
            await ParliamentStubs.First().Initialize.SendAsync(new Parliament.InitializeInput());
            var defaultOrganizationAddress =
                await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(ConsensusStub.ChangeSideChainConsensusInformationController),
                Params = new AuthorityInfo
                {
                    OwnerAddress = defaultOrganizationAddress,
                    ContractAddress = ContractAddresses[ParliamentSmartContractAddressNameProvider.Name]
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress
            });

            var controller = await ConsensusStub.GetSideChainConsensusInformationController.CallAsync(new Empty());
            controller.OwnerAddress.ShouldBe(defaultOrganizationAddress);
        }

        [Fact]
        public async Task SideChainDividendPoolTest()
        {
            InitialContracts();

            await ConsensusStub.SetSymbolList.SendWithExceptionAsync(new SymbolList());
            await ConsensusStub.GetDividends.CallAsync(new Int64Value {Value = 1});
        }
    }
}