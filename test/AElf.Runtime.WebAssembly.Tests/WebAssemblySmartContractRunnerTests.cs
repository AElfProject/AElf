using AElf.Types;
using Google.Protobuf;
using Shouldly;

namespace AElf.Runtime.WebAssembly.Tests;

public class WebAssemblySmartContractRunnerTests : WebAssemblyRuntimeTestBase
{
    [Fact]
    public async Task Run_Test()
    {
        const string solFilePath = "solFiles/simple.sol";

        var contractCode = await File.ReadAllBytesAsync(solFilePath);
        var smartContractRegistration = new SmartContractRegistration
        {
            Code = ByteString.CopyFrom(contractCode),
            CodeHash = HashHelper.ComputeFrom(contractCode),
            IsSystemContract = true
        };

        var smartContractRunner = new UnitTestWebAssemblySmartContractRunner();

        var executive = await smartContractRunner.RunAsync(smartContractRegistration);
        executive.ShouldNotBe(null);
    }
}