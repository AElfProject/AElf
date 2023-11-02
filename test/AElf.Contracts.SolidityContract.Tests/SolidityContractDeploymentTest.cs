using System.IO;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Shouldly;

namespace AElf.Contracts.SolidityContract;

public class SolidityContractDeploymentTest : SolidityContractTestBase
{
    [Fact]
    public async Task<Address> DeployStorageContract()
    {
        var codeBytes = await File.ReadAllBytesAsync("contracts/Storage.sol");
        var executionResult = await DeploySolidityContractAsync(codeBytes);
        executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var contractAddress = executionResult.Output;
        var registration = await BasicContractZeroStub.GetSmartContractRegistrationByAddress.CallAsync(contractAddress);
        registration.Category.ShouldBe(KernelConstants.SolidityRunnerCategory);
        registration.Code.ShouldBe(ByteString.CopyFrom(codeBytes));
        return contractAddress;
    }
}