using AElf.Kernel;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Runtime.WebAssembly.Extensions;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Nethereum.ABI;
using Shouldly;
using Solang;

namespace AElf.Runtime.WebAssembly.Tests;

public class ExecutiveTests : WebAssemblyRuntimeTestBase
{
    private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;

    public ExecutiveTests()
    {
        _hostSmartContractBridgeContextService = GetRequiredService<IHostSmartContractBridgeContextService>();
    }

    [Theory]
    [InlineData(1023, "00")]
    [InlineData(1024, "01")]
    [InlineData(16, "01")]
    [InlineData(33, "00")]
    public async Task IsPowerOf2Test(ulong input, string output)
    {
        const string solFilePath = "solFiles/simple.sol";
        const string functionName = "is_power_of_2(uint256)";

        var executive = CreateExecutive(solFilePath);
        var parameter = new ABIEncode().GetABIEncoded(new ABIValue("uint256", input));
        var txContext = MockTransactionContext(functionName, ByteString.CopyFrom(parameter));

        var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
        executive.SetHostSmartContractBridgeContext(hostSmartContractBridgeContext);
        await executive.ApplyAsync(txContext);

        var returnValue = txContext.Trace.ReturnValue;
        returnValue.ShouldNotBeNull();
        returnValue.ToHex().ShouldBe(output);
    }

    [Fact(DisplayName = "Input / SealReturn works.")]
    public async Task FooTest()
    {
        const string solFilePath = "solFiles/simple.sol";
        const string functionName = "foo()";
        var executive = CreateExecutive(solFilePath);
        var txContext = MockTransactionContext(functionName);

        var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
        executive.SetHostSmartContractBridgeContext(hostSmartContractBridgeContext);

        await executive.ApplyAsync(txContext);
        var hexReturn = txContext.Trace.ReturnValue.ToHex();
        hexReturn.ShouldBe("02000000");
    }

    [Fact(DisplayName = "GetStorage / SetStorage works.")]
    public async Task BarTest()
    {
        const string solFilePath = "solFiles/simple.sol";
        const string functionName = "bar()";
        var executive = CreateExecutive(solFilePath);
        var txContext = MockTransactionContext(functionName);

        var hostSmartContractBridgeContext = _hostSmartContractBridgeContextService.Create();
        executive.SetHostSmartContractBridgeContext(hostSmartContractBridgeContext);

        await executive.ApplyAsync(txContext);
        txContext.Trace.ReturnValue.ToHex().ShouldBe("02000000");
    }

    public async Task StorageTest()
    {
        const string solFilePath = "solFiles/Srorage.sol";
        
    }

    private IExecutive CreateExecutive(string solFilePath)
    {
        var code = File.ReadAllBytes(solFilePath);
        var wasmCode = new Compiler().BuildWasm(code).Contracts.First().WasmCode.ToByteArray();
        var executive = new Executive(new UnitTestExternalEnvironment(), wasmCode);
        return executive;
    }
}