using System.Linq;
using System.Threading.Tasks;
using AElf.ContractDeployer;
using AElf.Contracts.TestContract.Vote;
using AElf.Kernel;
using AElf.Standards.ACS0;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AEDPoSExtension.Demo.Tests;

public class LongStateKeyTests : AEDPoSExtensionDemoTestBase
{
    private const int SsdbMaxKeyLength = 200;

    [Fact]
    public async Task CallLongStateKeyMethodTest()
    {
        var testContractStub = await DeployTestVoteContractAsync();
        var input = new AddOptionInput
        {
            VotingItemId = HashHelper.ComputeFrom("TestVotingItem"),
            Option = HashHelper.ComputeFrom("TestOption"),
        };
        var result = await testContractStub.AddOption.SendAsync(input);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var trace = TransactionTraceProvider.GetTransactionTrace(result.TransactionResult.TransactionId);
        trace.StateSet.Writes.Count.ShouldBe(1);
        trace.StateSet.Writes.First().Key.Length.ShouldBeGreaterThan(SsdbMaxKeyLength);

        var chain = await BlockchainService.GetChainAsync();
        await BlockchainStateService.MergeBlockStateAsync(chain.BestChainHeight, chain.BestChainHash);
        //var task = BlockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash);

        // Also readable.
        var state = await testContractStub.GetState.CallAsync(input);
        state.Value.Length.ShouldBeGreaterThan(SsdbMaxKeyLength);

        var stateKey = GetStateKey(input);
        // Won't get value via state key directly.
        (await VersionedStates.GetAsync(stateKey)).ShouldBeNull();

        // Will get value via hashed state key.
        var key = HashHelper.ComputeFrom(stateKey).ToHex();
        var versionedState = await VersionedStates.GetAsync(key);
        var expectedValue = RepeatStringMultipleTimes(input.Option.ToHex(), 5);
        versionedState.Key.ShouldBe(stateKey);
        versionedState.Value.ToStringUtf8().ShouldBe(expectedValue);
    }

    private string GetStateKey(AddOptionInput input)
    {
        return
            $"2pPg7R7huHFMBZAZHquvpDj9bxNZcEEJEZ7uduYUvhwzKnKWDM/State/{RepeatStringMultipleTimes(input.VotingItemId.ToHex(), 5)}/{RepeatStringMultipleTimes(input.Option.ToHex(), 5)}";
    }

    private string RepeatStringMultipleTimes(string input, int times)
    {
        return string.Concat(Enumerable.Repeat(input, times));
    }

    private async Task<VoteContractContainer.VoteContractStub>
        DeployTestVoteContractAsync()
    {
        var address = (await BasicContractZeroStub.DeploySmartContract.SendAsync(new ContractDeploymentInput
        {
            Category = KernelConstants.DefaultRunnerCategory,
            Code = ByteString.CopyFrom(ContractsDeployer.GetContractCodes<AEDPoSExtensionDemoModule>()
                .Single(kv => kv.Key.EndsWith("TestContract.Vote"))
                .Value)
        })).Output;

        return GetTester<VoteContractContainer.VoteContractStub>(address,
            Accounts[0].KeyPair);
    }
}