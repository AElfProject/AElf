using System.IO;
using System.Threading.Tasks;
using AElf.Types;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class BallotContractTests : SolidityContractTestBase
{
    [Fact]
    public async Task<Address> DeployBallotContractTest()
    {
        const string solFilePath = "contracts/Ballot.sol";
        var executionResult = await DeployWebAssemblyContractAsync(await File.ReadAllBytesAsync(solFilePath));
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return executionResult.Output;
    }
}