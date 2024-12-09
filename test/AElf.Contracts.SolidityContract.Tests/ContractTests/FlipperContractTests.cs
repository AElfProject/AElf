using System.Threading.Tasks;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class FlipperContractTests : SolidityContractTestBase
{
    public FlipperContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        ContractPath = "contracts/flipper.contract";
    }

    [Fact(DisplayName = "Set init value to true.")]
    public async Task InitValueTrueTest()
    {
        var contractAddress = await DeployContractAsync(BoolType.True);
        {
            var value = await QueryAsync(contractAddress, "get");
            value.ShouldBe(BoolType.True);
        }
    }

    [Fact(DisplayName = "Set init value to false and flip it.")]
    public async Task InitValueFalseTest()
    {
        var contractAddress = await DeployContractAsync(BoolType.False);
        {
            var value = await QueryAsync(contractAddress, "get");
            value.ShouldBe(BoolType.False);
        }
        await ExecuteTransactionAsync(contractAddress, "flip");
        {
            var value = await QueryAsync(contractAddress, "get");
            value.ShouldBe(BoolType.True);
        }
    }
}