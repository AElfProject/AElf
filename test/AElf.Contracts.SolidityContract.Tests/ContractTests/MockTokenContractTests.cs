using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Runtime.WebAssembly.Types;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;
using Xunit.Abstractions;

namespace AElf.Contracts.SolidityContract;

public class MockTokenContractTests : ERC20ContractTests
{
    private readonly ITestOutputHelper _outputHelper;

    public MockTokenContractTests(ITestOutputHelper outputHelper) : base(outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task<Address> DeployMockTokenContract()
    {
        var wasmCode = await LoadWasmContractCode("contracts/MockToken.contract");
        var executionResult = await DeployWasmContractAsync(wasmCode);
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var contractAddress = executionResult.Output;
        return contractAddress;
    }

    [Fact]
    public async Task<Address> InitializeMockTokenContract()
    {
        var contractAddress = await DeployMockTokenContract();
        var elf = ByteArrayHelper.HexStringToByteArray("0x0c454c46");
        var elfToken = ByteArrayHelper.HexStringToByteArray("0x24456c6620746f6b656e");
        var tx = await GetTransactionAsync(AliceKeyPair, contractAddress, "initialize",
            ByteString.CopyFrom(elfToken.Concat(elf).ToArray()));
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        Encoding.UTF8.GetString((await QueryField(contractAddress, "symbol"))
            .ToByteArray()).ShouldContain("ELF");
        return contractAddress;
    }
}