using System.Threading.Tasks;
using AElf.Types;
using NBitcoin.RPC;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class CreateContractTests : SolidityContractTestBase
{
    public CreateContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        ContractPath = "contracts/creator.contract";
    }
    
    [Fact]
    public async Task CreateContract()
    {
        var childContractHash = await UploadContractAsync("contracts/child_create_contract.contract");
        childContractHash.ShouldNotBeNull();
        var contractAddress = await DeployContractAsync();
        {
            var executionResult = await ExecuteTransactionAsync(contractAddress, "create_child");
            executionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        {
            var executionResult = await QueryAsync(contractAddress, "call_child");
            StringType.From(executionResult.ToByteArray()).ToString().ShouldBe("child");
        }
    }
}