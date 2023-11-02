using System.IO;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class CallContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task CallTest()
    {
        Address calleeContractAddress, callerContractAddress;
        {
            const string solFilePath = "contracts/call_callee.sol";
            var executionResult = await DeploySolidityContractAsync(await File.ReadAllBytesAsync(solFilePath));
            calleeContractAddress = executionResult.Output;
        }
        {
            const string solFilePath = "contracts/call_caller.sol";
            var executionResult = await DeploySolidityContractAsync(await File.ReadAllBytesAsync(solFilePath));
            callerContractAddress = executionResult.Output;
        }

        var input = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(new ABIValue("bytes32", calleeContractAddress.ToByteArray())));
        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, callerContractAddress, "testCall",
                input);
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        // Checks
        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, callerContractAddress, "callee");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            txResult.ReturnValue.ToHex().ShouldBe(calleeContractAddress.ToByteArray().ToHex());
        }

        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, calleeContractAddress, "add");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            txResult.ReturnValue.ToHex().ShouldBe(calleeContractAddress.ToByteArray().ToHex());
        }
    }
}