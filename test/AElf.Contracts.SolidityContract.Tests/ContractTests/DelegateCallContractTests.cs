using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class DelegateCallContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task DelegateCallTest()
    {
        const long vars = 1616;
        const long transferValue = 100;
        Address delegateeContractAddress, delegatorContractAddress;
        {
            const string solFilePath = "contracts/delegate_call_delegatee.sol";
            var executionResult = await DeployWasmContractAsync(await File.ReadAllBytesAsync(solFilePath));
            delegateeContractAddress = executionResult.Output;
        }
        {
            const string solFilePath = "contracts/delegate_call_delegator.sol";
            var executionResult = await DeployWasmContractAsync(await File.ReadAllBytesAsync(solFilePath));
            delegatorContractAddress = executionResult.Output;
        }

        var input = WebAssemblyTypeHelper.ConvertToParameter(delegateeContractAddress.ToWebAssemblyAddress(),
            vars.ToWebAssemblyUInt256());

        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, delegatorContractAddress, "setVars",
                input, transferValue);
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // Checks
        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, delegatorContractAddress, "num");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            txResult.ReturnValue.ToByteArray().ToInt64(false).ShouldBe(vars);
        }

        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, delegatorContractAddress, "value");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            txResult.ReturnValue.ToByteArray().ToInt64(false).ShouldBe(transferValue);
        }
    }
}