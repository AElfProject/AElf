using AElf.Contracts.Configuration;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeatureDisable.Core;
using AElf.Standards.ACS3;
using AElf.TestBase;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.FeatureDisable.Tests;

public class FeatureDisableTest : KernelFeatureDisableTestBase
{
    private readonly IMockService _mockService;
    private readonly IBlockchainService _blockchainService;

    public FeatureDisableTest()
    {
        _mockService = GetRequiredService<IMockService>();
        _blockchainService = GetRequiredService<IBlockchainService>();
    }

    [Fact]
    public async Task IsFeatureDisabledTest()
    {
        await DeployContractsAsync();
        await ConfigDisabledFeaturesAsync("FeatureA, FeatureB, FeatureBAndC");
        (await _mockService.IsFeatureADisabledAsync()).ShouldBeTrue();
        (await _mockService.IsFeatureBDisabledAsync()).ShouldBeTrue();
        (await _mockService.IsFeatureCDisabledAsync()).ShouldBeTrue();
        (await _mockService.IsFeatureDDisabledAsync()).ShouldBeFalse();
    }

    private async Task ConfigDisabledFeaturesAsync(string disableFeatureNames)
    {
        var chain = await _blockchainService.GetChainAsync();
        await _blockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash);
        var proposalId = (await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ContractMethodName = "SetConfiguration",
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            Params = new SetConfigurationInput
            {
                Key = FeatureDisableConstants.FeatureDisableConfigurationName,
                Value = new StringValue { Value = disableFeatureNames }.ToByteString()
            }.ToByteString(),
            ToAddress = ConfigurationContractAddress,
            OrganizationAddress = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty())
        })).Output;
        await ParliamentContractStub.Approve.SendAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);
    }
}