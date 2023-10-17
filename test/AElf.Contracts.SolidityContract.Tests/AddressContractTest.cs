using System.IO;
using System.Threading.Tasks;
using AElf.Types;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class AddressContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task AddressTest()
    {
        const string solFilePath = "contracts/Address.sol";
        var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
        var contractAddress = executionResult.Output;
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "getAddress");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ShouldBe(DefaultSender.Value);
    }
}