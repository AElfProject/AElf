using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        var input = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(
            new ABIValue("bytes32", delegateeContractAddress.ToByteArray()),
            new ABIValue("uint256", 1616)));
        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, delegatorContractAddress, "setVars",
                input, 100);
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // Checks
        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, delegatorContractAddress, "num");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var num = new ABIEncode().GetABIEncoded(new ABIValue("uint256", 1616));
            txResult.ReturnValue.ShouldBe(num);
        }

        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, delegatorContractAddress, "value");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var value = new ABIEncode().GetABIEncoded(new ABIValue("uint256", 100));
            txResult.ReturnValue.Reverse().ShouldBe(value);
        }
    }
}