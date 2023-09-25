using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Nethereum.ABI;
using Shouldly;

namespace AElf.Runtime.WebAssembly.Tests;

public class WebAssemblySmartContractRunnerTests : WebAssemblyRuntimeTestBase
{
    private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;
    private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;

    public WebAssemblySmartContractRunnerTests()
    {
        _hostSmartContractBridgeContextService = GetRequiredService<IHostSmartContractBridgeContextService>();
        _smartContractRunnerContainer = GetRequiredService<ISmartContractRunnerContainer>();
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

        var smartContractRunner = _smartContractRunnerContainer.GetRunner(KernelConstants.SolidityRunnerCategory);
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