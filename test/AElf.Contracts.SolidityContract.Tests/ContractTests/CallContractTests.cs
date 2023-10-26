using System.IO;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.ABI.Encoders;
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
            var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
            calleeContractAddress = executionResult.Output;
        }
        {
            const string solFilePath = "contracts/call_caller.sol";
            var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
            callerContractAddress = executionResult.Output;
        }

        // var address = Address.FromBytes(calleeContractAddress.Value.ToByteArray().Take(20).ToArray().RightPad(32));
        var address = new AddressTypeEncoder().Encode(calleeContractAddress.AElfAddressToEthAddress()).ToHex();
        var input = ByteString.CopyFrom(new ABIEncode().GetABIEncoded(new ABIValue("address", address)));
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
            txResult.ReturnValue.ToHex().ShouldBe(address);
        }

        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, calleeContractAddress, "add");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            txResult.ReturnValue.ToHex().ShouldBe(calleeContractAddress.ToByteArray().ToHex());
        }
    }
}