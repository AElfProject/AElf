using System.IO;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
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
    public async Task<Address> ConstructorTest()
    {
        var contractAddress = await DeployOwnerContractTest();
        var tx = GetTransaction(DefaultSenderKeyPair, contractAddress, "deploy");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return contractAddress;
    }

    [Fact]
    public async Task<Address> GetOwnerTest()
    {
        var contractAddress = await ConstructorTest();
        var tx = GetTransaction(DefaultSenderKeyPair, contractAddress, "getOwner()".ToSelector());
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ShouldNotBeEmpty();
        return contractAddress;
    }

    [Fact]
    public async Task<Address> ChangeOwnerTest()
    {
        var contractAddress = await ConstructorTest();
        var tx = GetTransaction(DefaultSenderKeyPair, contractAddress, "changeOwner(address)".ToSelector(),
            ByteString.CopyFrom(
                new ABIEncode().GetABIEncoded(new ABIValue("address", "0x0000000000000000000000000000000000000000"))));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
        return contractAddress;
    }
}