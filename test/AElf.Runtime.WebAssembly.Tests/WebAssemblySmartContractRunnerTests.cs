using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
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
            CodeHash = HashHelper.ComputeFrom(contractCode)
        };

        var smartContractRunner = new UnitTestWebAssemblySmartContractRunner(new UnitTestExternalEnvironment());
        var executive = await smartContractRunner.RunAsync(smartContractRegistration);
        executive.ContractHash.ShouldNotBeNull();

        // executive should works.
        const string functionName = "is_power_of_2(uint256)";
        var parameter = new ABIEncode().GetABIEncoded(new ABIValue("uint256", 1024));
        var txContext = MockTransactionContext(functionName, ByteString.CopyFrom(parameter));
        await executive.ApplyAsync(txContext);
        var returnValue = txContext.Trace.ReturnValue;
        returnValue.ToHex().ShouldBe("01");
    }
}