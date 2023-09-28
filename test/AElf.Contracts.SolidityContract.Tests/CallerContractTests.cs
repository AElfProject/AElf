using System.IO;
using System.Threading.Tasks;
using AElf.Types;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class CallerContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task<Address> CallerTest()
    {
        const string solFilePath = "contracts/caller.sol";
        var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
        var contractAddress = executionResult.Output;
        var tx = await GetTransactionAsync(DefaultSenderKeyPair, contractAddress, "caller");
        var txResult = await TestTransactionExecutor.ExecuteAsync(tx);
        txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        txResult.ReturnValue.ToByteArray().ShouldBe(DefaultSender.ToByteArray());
        return contractAddress;
    }
    
}