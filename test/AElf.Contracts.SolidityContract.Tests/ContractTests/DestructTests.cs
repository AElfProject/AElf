using System.Threading.Tasks;
using AElf.Types;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class DestructTests : SolidityContractTestBase
{
    private readonly ITestOutputHelper _outputHelper;

    public DestructTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _outputHelper = outputHelper;
        ContractPath = "contracts/destruct.contract";
    }

    [Fact]
    public async Task HelloTest()
    {
        var contractAddress = await DeployContractAsync();
        var txResult = await ExecuteTransactionAsync(contractAddress, "hello");
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        StringType.From(txResult.ReturnValue.ToByteArray()).ToString().ShouldBe("Hello");
    }

    [Fact(Skip = "Not support selfdestruct yet.")]
    public async Task SelfterminateTest()
    {
        var contractAddress = await DeployContractAsync();
        var txResult = await ExecuteTransactionAsync(contractAddress, "selfterminate");
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }
}