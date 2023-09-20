using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Runtime.WebAssembly.Tests.MockedExternalEnvironment;

public class WebAssemblySmartContractRunnerTests : WebAssemblyRuntimeTestBase
{
    private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;

    public WebAssemblySmartContractRunnerTests()
    {
        _hostSmartContractBridgeContextService = GetRequiredService<IHostSmartContractBridgeContextService>();
    }

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

        var smartContractRunner = new WebAssemblySmartContractRunner(new UnitTestExternalEnvironment());
        var executive = await smartContractRunner.RunAsync(smartContractRegistration);
        executive.ContractHash.ShouldNotBeNull();

        // executive should works.
        const string functionName = "1d763c2a"; //is_power_of_2
        var parameter = new ABIEncode().GetABIEncoded(new ABIValue("uint256", 1024));
        var txContext = MockTransactionContext(functionName, ByteString.CopyFrom(parameter));

        var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
        executive.SetHostSmartContractBridgeContext(hostSmartContractBridgeContext);
        
        await executive.ApplyAsync(txContext);
        var returnValue = txContext.Trace.ReturnValue;
        returnValue.ToHex().ShouldBe("01");
    }
}