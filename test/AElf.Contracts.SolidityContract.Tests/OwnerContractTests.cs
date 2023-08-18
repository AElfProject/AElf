using System.IO;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class OwnerContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task<Address> DeployOwnerContractTest()
    {
        const string solFilePath = "contracts/Owner.sol";
        var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return executionResult.Output;
    }

    [Fact]
    public async Task<Address> GetOwnerTest()
    {
        var contractAddress = await DeployOwnerContractTest();
        var tx = GetTransaction(DefaultSenderKeyPair, contractAddress, "getOwner()".ToSelector());
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ShouldBe(DefaultSender.ToByteArray());
        return contractAddress;
    }

    [Fact]
    public async Task ChangeOwnerTest()
    {
        var contractAddress = await DeployOwnerContractTest();

        const string newAddress = "0x0000000000000000000000000000000000000000";
        {
            var tx = GetTransaction(DefaultSenderKeyPair, contractAddress, "changeOwner(address)".ToSelector(),
                ByteString.CopyFrom(
                    new ABIEncode().GetABIEncoded(new ABIValue("address", newAddress))));
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var tx = GetTransaction(DefaultSenderKeyPair, contractAddress, "getOwner()".ToSelector());
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.ReturnValue.ToHex(true).ShouldContain(newAddress);
        }
    }
}