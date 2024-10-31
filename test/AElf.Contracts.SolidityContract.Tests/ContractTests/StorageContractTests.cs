using System.IO;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Scale;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class StorageContractTests : SolidityContractTestBase
{
    public StorageContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        
    }

    [Fact]
    public async Task<Address> StoreTest()
    {
        const string solFilePath = "contracts/Storage.sol";
        var executionResult = await DeployWasmContractAsync(await File.ReadAllBytesAsync(solFilePath));
        var contractAddress = executionResult.Output;
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "store",
            UInt256Type.GetByteStringFrom(100));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return contractAddress;
    }

    [Fact]
    public async Task RetrieveTest()
    {
        var contractAddress = await StoreTest();
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "retrieve");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ToByteArray().ToInt64(false).ShouldBe(100);
    }
}