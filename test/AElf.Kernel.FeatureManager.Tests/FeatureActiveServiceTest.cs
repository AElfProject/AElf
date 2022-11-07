using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Standards.ACS3;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.FeatureManager.Tests;

public class FeatureActiveServiceTest : KernelFeatureManagerTestBase
{
    private readonly IBlockchainService _blockchainService;
    private readonly IMockService _mockService;

    public FeatureActiveServiceTest()
    {
        _mockService = GetRequiredService<IMockService>();
        _blockchainService = GetRequiredService<IBlockchainService>();
    }

    [Fact]
    public async Task FeatureManageTest()
    {
        await DeployContractsAsync();

        {
            var currentHeight = await GetCurrentHeight();
            await ConfigFeatureActiveHeight("Version2", currentHeight + 5);
        }

        {
            var currentFeature = await _mockService.GetCurrentFeatureNameAsync();
            currentFeature.ShouldBe("Version1");
        }

        {
            var currentHeight = await GetCurrentHeight();
            await ConfigFeatureActiveHeight("Version3", currentHeight + 5);
        }

        {
            var currentFeature = await _mockService.GetCurrentFeatureNameAsync();
            currentFeature.ShouldBe("Version2");
        }

        {
            var currentHeight = await GetCurrentHeight();
            await ConfigFeatureActiveHeight("Version3", currentHeight + 5);
        }

        {
            var currentFeature = await _mockService.GetCurrentFeatureNameAsync();
            currentFeature.ShouldBe("Version2");
        }

        {
            var currentHeight = await GetCurrentHeight();
            await ConfigFeatureActiveHeight("Version3", currentHeight + 1);
        }

        {
            var currentFeature = await _mockService.GetCurrentFeatureNameAsync();
            currentFeature.ShouldBe("Version3");
        }

        {
            var currentHeight = await GetCurrentHeight();
            await ConfigFeatureActiveHeight("Version3", currentHeight + 5);
        }

        {
            var currentFeature = await _mockService.GetCurrentFeatureNameAsync();
            currentFeature.ShouldBe("Version2");
        }
    }

    private async Task ConfigFeatureActiveHeight(string featureName, long activeHeight)
    {
        var chain = await _blockchainService.GetChainAsync();
        await _blockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash);
        var proposalId = (await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ContractMethodName = "SetConfiguration",
            ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
            Params = new SetConfigurationInput
            {
                Key = $"{FeatureManagerConstants.FeatureConfigurationNamePrefix}{featureName}",
                Value = new Int64Value { Value = activeHeight }.ToByteString()
            }.ToByteString(),
            ToAddress = ConfigurationContractAddress,
            OrganizationAddress = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty())
        })).Output;
        await ParliamentContractStub.Approve.SendAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);
    }

    private async Task<long> GetCurrentHeight()
    {
        var chain = await _blockchainService.GetChainAsync();
        return chain.BestChainHeight;
    }
}