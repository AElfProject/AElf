using System;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.ContractTestKit;
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
    public class MaximumMinersCountTests : AEDPoSExtensionDemoTestBase
    {
        [Theory]
        [InlineData(7)]
        [InlineData(3)]
        public async Task SetMaximumMinersCountTest(int targetMinersCount)
        {
            await BlockMiningService.MineBlockToNextTermAsync();

            InitialAcs3Stubs();
            await ParliamentStubs.First().Initialize.SendAsync(new Parliament.InitializeInput());
            var defaultOrganizationAddress =
                await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(ConsensusStub.SetMaximumMinersCount),
                Params = new Int32Value
                {
                    Value = targetMinersCount
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress
            });

            {
                var currentMinersCount = (await ConsensusStub.GetCurrentMinerList.CallAsync(new Empty())).Pubkeys.Count;
                currentMinersCount.ShouldBe(5);
                var currentTermNumber = await BlockMiningService.MineBlockToNextTermAsync();
                currentTermNumber.ShouldBe(3);
            }

            {
                var currentMinersCount = (await ConsensusStub.GetCurrentMinerList.CallAsync(new Empty())).Pubkeys.Count;
                currentMinersCount.ShouldBe(Math.Min(targetMinersCount, 5));
                var currentTermNumber = await BlockMiningService.MineBlockToNextTermAsync();
                currentTermNumber.ShouldBe(4);
            }
            
            var maxMinersCount = await ConsensusStub.GetMaximumMinersCount.CallAsync(new Empty());

        }

        [Fact]
        public async Task ChangeMaximumMinersCountControllerTest()
        {
            await BlockMiningService.MineBlockToNextTermAsync();

            InitialAcs3Stubs();
            await ParliamentStubs.First().Initialize.SendAsync(new Parliament.InitializeInput());
            var targetAddress = await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());

            var defaultOrganizationAddress =
                await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
            await ParliamentReachAnAgreementAsync(new CreateProposalInput
            {
                ToAddress = ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
                ContractMethodName = nameof(ConsensusStub.ChangeMaximumMinersCountController),
                Params = new AuthorityInfo
                {
                    OwnerAddress = targetAddress,
                    ContractAddress = ContractAddresses[ParliamentSmartContractAddressNameProvider.Name]
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress
            });

            var newMinersCountController = await ConsensusStub.GetMaximumMinersCountController.CallAsync(new Empty());
            newMinersCountController.OwnerAddress.ShouldBe(targetAddress);
        }
    }
}