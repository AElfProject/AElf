using System.Threading.Tasks;
using AElf.Types;
using Scale;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public sealed class AssertsTest : SolidityContractTestBase
{
    public AssertsTest()
    {
        ContractPath = "contracts/asserts.contract";
    }

    [Fact(DisplayName = "Test asserts contract.")]
    public async Task TestAsserts()
    {
        var contractAddress = await DeployContractAsync();

        // Query var.
        {
            var queriedVar = await QueryAsync(contractAddress, "var");
            Int64Type.From(queriedVar.ToByteArray()).Value.ShouldBe(1);
        }

        // Test test_assert_rpc method.
        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "test_assert_rpc");
            var txResult = await TestTransactionExecutor.ExecuteWithExceptionAsync(tx);
            txResult.Error.ShouldContain("runtime_error: I refuse revert encountered in asserts.sol");
            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        // Test test_assert method.
        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "test_assert");
            var txResult = await TestTransactionExecutor.ExecuteWithExceptionAsync(tx);
            txResult.Error.ShouldContain("runtime_error: assert failure");
            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        // var is still 1.
        {
            var queriedVar = await QueryAsync(contractAddress, "var");
            Int64Type.From(queriedVar.ToByteArray()).Value.ShouldBe(1);
        }
    }
}