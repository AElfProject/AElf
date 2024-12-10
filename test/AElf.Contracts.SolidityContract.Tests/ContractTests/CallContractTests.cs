using System.IO;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class CallContractTests : SolidityContractTestBase
{
    public CallContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {

    }

    [Fact]
    public async Task CallTest()
    {
        Address calleeContractAddress, callerContractAddress;
        {
            const string solFilePath = "contracts/call_callee.sol";
            var executionResult = await DeployWasmContractAsync(await File.ReadAllBytesAsync(solFilePath));
            calleeContractAddress = executionResult.Output;
        }
        {
            const string solFilePath = "contracts/call_caller.sol";
            var executionResult = await DeployWasmContractAsync(await File.ReadAllBytesAsync(solFilePath));
            callerContractAddress = executionResult.Output;
        }

        {
            var txResult = await ExecuteTransactionAsync(callerContractAddress, "testCall",
                AddressType.GetByteStringFrom(calleeContractAddress));
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var callee = await QueryAsync(callerContractAddress, "callee");
            AddressType.From(callee.ToByteArray()).Value.ShouldBe(calleeContractAddress);
            var add = await QueryAsync(calleeContractAddress, "add");
            AddressType.From(add.ToByteArray()).Value.ShouldBe(calleeContractAddress);
        }

        {
            var txResult = await ExecuteTransactionAsync(callerContractAddress, "testDelegatecall",
                AddressType.GetByteStringFrom(calleeContractAddress));
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var add = await QueryAsync(calleeContractAddress, "add");
            AddressType.From(add.ToByteArray()).Value.ShouldBe(calleeContractAddress);
        }
    }
}