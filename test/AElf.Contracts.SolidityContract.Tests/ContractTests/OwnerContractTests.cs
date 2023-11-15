using System.IO;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Runtime.WebAssembly.Types;
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
        var executionResult = await DeployWasmContractAsync(await File.ReadAllBytesAsync(solFilePath));
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        executionResult.TransactionResult.Logs.Count.ShouldBePositive();
        return executionResult.Output;
    }

    [Fact]
    public async Task CallConstructorTwice()
    {
        var contractAddress = await DeployOwnerContractTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "deploy");
        var txResult = await TestTransactionExecutor.ExecuteWithExceptionAsync(tx);
        txResult.Error.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task<Address> GetOwnerTest()
    {
        var contractAddress = await DeployOwnerContractTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "getOwner");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ShouldBe(DefaultSender.Value);
        return contractAddress;
    }

    [Fact]
    public async Task ChangeOwnerTest()
    {
        var contractAddress = await DeployOwnerContractTest();

        var newAddress = SampleAccount.Accounts[1].Address.ToByteArray();

        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "changeOwner",
                newAddress.ToWebAssemblyAddress().ToParameter());
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "getOwner");
            var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
            txResult.ReturnValue.ShouldBe(newAddress);
        }
    }
}