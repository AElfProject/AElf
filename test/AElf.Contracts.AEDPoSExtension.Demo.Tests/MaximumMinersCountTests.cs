using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Parliament;
using AElf.ContractTestKit;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Proposal;
using AElf.Standards.ACS3;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests;

public class MaximumMinersCountTests : AEDPoSExtensionDemoTestBase
{
    private IBlockTimeProvider _blockTimeProvider =>
        Application.ServiceProvider.GetRequiredService<IBlockTimeProvider>();
    
    [Theory]
    [InlineData(7)]
    [InlineData(3)]
    public async Task SetMaximumMinersCountTest(int targetMinersCount)
    {
        InitialContracts();
        await BlockMiningService.MineBlockToNextTermAsync();

        InitialAcs3Stubs();
        await ParliamentStubs.First().Initialize.SendAsync(new InitializeInput());
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
        maxMinersCount.Value.ShouldBe(targetMinersCount);

        var minedBlocksOfPreviousTerm = await ConsensusStub.GetMinedBlocksOfPreviousTerm.CallAsync(new Empty());
        minedBlocksOfPreviousTerm.Value.ShouldBeGreaterThan(200);

        var previousMinerList = await ConsensusStub.GetPreviousMinerList.CallAsync(new Empty());
        previousMinerList.Pubkeys.Count.ShouldBePositive();
    }

    [Fact]
    public async Task ChangeMaximumMinersCountControllerTest()
    {
        InitialContracts();

        await BlockMiningService.MineBlockToNextTermAsync();

        InitialAcs3Stubs();
        await ParliamentStubs.First().Initialize.SendAsync(new InitializeInput());
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
    
    [Theory]
    [InlineData(31536000)]
    [InlineData(63072000)]
    public async Task SetMinerIncreaseIntervalTest(int targetMinerIncreaseInterval)
    {
        var increaseInterval = 31536000 * 2;
        var minerCount = 17;
        InitialContracts();
        await BlockMiningService.MineBlockToNextTermAsync();

        InitialAcs3Stubs();
        var maximumMinersCount = await ConsensusStub.GetMaximumMinersCount.CallAsync(new Empty());
        maximumMinersCount.Value.ShouldBe(minerCount);
        
        await ParliamentStubs.First().Initialize.SendAsync(new InitializeInput());
        var defaultOrganizationAddress =
            await ParliamentStubs.First().GetDefaultOrganizationAddress.CallAsync(new Empty());
        await ParliamentReachAnAgreementAsync(new CreateProposalInput
        {
            ToAddress = ContractAddresses[ConsensusSmartContractAddressNameProvider.Name],
            ContractMethodName = nameof(ConsensusStub.SetMinerIncreaseInterval),
            Params = new Int64Value
            {
                Value = targetMinerIncreaseInterval
            }.ToByteString(),
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            OrganizationAddress = defaultOrganizationAddress
        });
        var minerIncreaseInterval = await ConsensusStub.GetMinerIncreaseInterval.CallAsync(new Empty());
        minerIncreaseInterval.Value.ShouldBe(targetMinerIncreaseInterval);
        var blockTime = _blockTimeProvider.GetBlockTime();
        _blockTimeProvider.SetBlockTime(blockTime.AddSeconds(increaseInterval));
        maximumMinersCount = await ConsensusStub.GetMaximumMinersCount.CallAsync(new Empty());
        maximumMinersCount.Value.ShouldBe(minerCount+increaseInterval/targetMinerIncreaseInterval*2);
    }
}